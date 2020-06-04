namespace Vhmc.Pi.Types

open Unosquare.RaspberryIO.Abstractions
open System
open System.IO
open System.Configuration
open Unosquare.RaspberryIO
open Unosquare.WiringPi
open System.Threading
open Vhmc.Pi.Domain
open Vhmc.Pi.Common



[<AutoOpen>]
module Sinlgeton =

    type Global internal () =

        let mutable audioProfile = AudioProfile.Empty
        let mutable printAsyncOutput = false
        let mutable printAsyncAllLectures = false

        member val AudioProfile             = audioProfile              with get, set
        member val PrintAsyncOutput         = printAsyncOutput          with get, set
        member val PrintAsyncAllLectures    = printAsyncAllLectures     with get, set

    let Global = Global()


[<AutoOpen>]
module private ProcessInitialize =

    do
        Pi.Init<BootstrapWiringPi>()
    
    // Read intervals
    let timer = int ConfigurationManager.AppSettings.["AudioLectureSampleMS"]

    // Pwm
    let pwmMinRange = 0
    let pwmMaxRange = 100
    
    // i2c configuration
    let accuracy = 255 // 8 bits
                
    // Functions
    let calcDutyCycle analogValue = (analogValue * float pwmMaxRange) / float accuracy
    
    // Configure I2C devices
    let adcAddress = int ConfigurationManager.AppSettings.["AdcAddress"] // run i2cdetect -y 1
    let adc = Pi.I2C.AddDevice(adcAddress)
    let audioEnvelopeChannel = 0 // Envelope channel

    // Low volume level considered ok to avoid unnecessarily volume increases
    let lowVolumeLevelConsideredMute = int ConfigurationManager.AppSettings.["LowVolumeLevelConsideredMute"]
    
    // Led to visualize the envelope input from the audio sensor
    let envelopeLed = 
        let led = (Pi.Gpio.[BcmPin.Gpio05]) :?> GpioPin
        led.PinMode <- GpioPinDriveMode.Output
        led.StartSoftPwm(pwmMinRange, pwmMaxRange)
        led.SoftPwmValue <- 0
        led


[<AutoOpen>]
module Process =

    type OutputLed () =
        member __.Led = envelopeLed
        member this.On () = this.Led.SoftPwmValue <- pwmMaxRange
        member this.Half () = this.Led.SoftPwmValue <- pwmMaxRange / 2
        member this.Off () = this.Led.SoftPwmValue <- pwmMinRange
        member this.Blink times =
            async{
                let sleep () = Thread.Sleep(200)
                this.Off()
                for _ in [1..times] do
                    this.On()
                    sleep()
                    this.Off()
                    sleep()
            } |> Async.Start

    type IRTrxCommands (irCommandsFile) =
        let irTrxPin = (int) BcmPin.Gpio18
        let irCommand pin irConfigFile instruction = sprintf "python irrp.py -p -g%d -f%s %s" pin irConfigFile instruction
        
        member __.volumeUp () = sendCommand <| (irCommand irTrxPin irCommandsFile "VolumeUp")
        member __.volumeDown () = sendCommand <| (irCommand irTrxPin irCommandsFile "VolumeDown")

        interface IDisposable with
            member __.Dispose () = ()

    type IRRecCommands (profileName: string) =
        let irRecPin = (int) BcmPin.Gpio17
        let irCommand pin irConfigFile = sprintf "python irrp.py -r -g%d -f%s VolumeUp VolumeDown" pin irConfigFile

        member __.startRecording () =
            let outputFile = sprintf "%s" profileName
            sendCommand <| irCommand irRecPin outputFile |> Async.RunSynchronously

    type Profiles () =

        let saveProfiles (profiles: AudioProfiles) = 
            let serializedProfiles = serialize profiles
            use sw = new StreamWriter(new FileStream(profilesFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize= 4096))
            sw.Write(serializedProfiles)

        do
            // Create profile file is doesn't exist
            if File.Exists profilesFile |> not
            then AudioProfiles.Empty |> saveProfiles
               
        let loadAudioProfiles () = 
            let deserializedProfiles =
                use sr = new StreamReader (profilesFile)
                try deserialize<AudioProfiles> (sr.ReadToEnd())
                with ex -> Error ex

            match deserializedProfiles with
            | Ok x -> x
            | Error x -> failwithf "ProfilesDeserializationError:/n%A" x

        member __.existProfile profileName = loadAudioProfiles().Profiles |> List.exists (fun x -> x.Name = profileName)

        member this.removeProfile (profile: AudioProfile) =
            match this.existProfile profile.Name with
            | true -> 
                    File.Delete(profile.IRFileName)
                    loadAudioProfiles().Profiles 
                    |> List.filter (fun x -> x.Name <> profile.Name)
                    |> fun filteredProfiles -> AudioProfiles.Init filteredProfiles
                    |> saveProfiles
                    |> Ok
            | false -> Error "ProfileNotFound"

        member this.createProfile (profile: AudioProfile) =
            match this.existProfile profile.Name with
            | false -> 
                    loadAudioProfiles()
                    |> fun x -> { x with Profiles = x.Profiles @ [profile] }
                    |> saveProfiles
                    |> Ok
            | true -> Error "ProfileAlreadyExists"

        member this.updateProfile (profile: AudioProfile) =
            match this.existProfile profile.Name with
            | true ->
                    let audioProfiles = loadAudioProfiles()
                    let newProfiles =
                        audioProfiles.Profiles
                        |> List.filter (fun x -> x.Name <> profile.Name)
                        |> fun x -> x @ [profile]
                    { audioProfiles with Profiles = newProfiles } 
                    |> saveProfiles
                    |> Ok
            | false -> Error "ProfileNotFound"

        member __.getProfile profileName =
            match loadAudioProfiles().Profiles |> List.tryFind (fun x -> x.Name = profileName) with
            | Some x -> Ok x
            | None -> Error "ProfileNotFound"

        member __.getAudioProfiles () = loadAudioProfiles()

    type AudioSensor () =

        let destroy () =
            printfn "Destroyed..."
            Thread.Sleep(10)
            envelopeLed.SoftPwmValue <- 0

        do 
            System.Console.CancelKeyPress |> Event.add (fun _ -> destroy()) // Ctrl+C to finish application

        let rec readAudio envelopePrev =
            async{
                Thread.Sleep timer
            
                let envelopeCur = int <| adc.ReadAddressByte audioEnvelopeChannel
                envelopeLed.SoftPwmValue <- int (calcDutyCycle (float envelopeCur))

                let printNext text =
                    if envelopeCur <> envelopePrev
                    then printf "\n%s : %d " text envelopeCur
                    else printf "."

                printNext ">"
                do! readAudio envelopeCur
            }

        member __.run () =
            try 
                use cancellationSource = new CancellationTokenSource()
                Async.Start((readAudio -1), cancellationSource.Token)

                Console.ReadKey() |> ignore
                cancellationSource.Cancel()

                Thread.Sleep(1000)
                OutputLed().Off()

            with ex -> printfn "%A" ex
                       destroy()

    type IrAudioLeveler (audioProfile: AudioProfile) =

        do
            Global.AudioProfile <- audioProfile

        do 
            printfn "Samples/second: %d" (1000 / timer)
            Global.AudioProfile.printValues()

        let trx = new IRTrxCommands(Global.AudioProfile.IRFileName)

        let destroy () =
            printfn "Destroyed..."
            trx :> IDisposable |> fun x -> x.Dispose()
            Thread.Sleep(100)
            Global.AudioProfile <- AudioProfile.Empty
            envelopeLed.SoftPwmValue <- 0

        do 
            System.Console.CancelKeyPress |> Event.add (fun _ -> destroy()) // Ctrl+C to finish application

        // TODO: Delete this function
        let rec readAudio levelDwCounter levelUpCounter envelopePrev =
            async{
                Thread.Sleep timer
            
                let envelopeCur = int <| adc.ReadAddressByte audioEnvelopeChannel

                envelopeLed.SoftPwmValue <- int (calcDutyCycle (float envelopeCur))

                let printNext text =
                    if Global.PrintAsyncOutput then
                        if envelopeCur <> envelopePrev
                        then printf "\n%s : %d " text envelopeCur
                        else printf "."

                try
                    match envelopeCur with
                    | x when x > Global.AudioProfile.SoundIdealUpperLimit -> 
                                        printNext "Up"
                                        if levelDwCounter < Global.AudioProfile.MaxIRDecreasesAllowed
                                        then trx.volumeDown() |> Async.RunSynchronously
                                             do! readAudio (levelDwCounter + 1) (levelUpCounter - 1) envelopeCur
                                        else do! readAudio levelDwCounter levelUpCounter envelopeCur
                    | x when x < Global.AudioProfile.SoundIdealBottomLimit && x > lowVolumeLevelConsideredMute -> 
                                        printNext "Dw"
                                        if levelUpCounter < Global.AudioProfile.MaxIRIncreasesAllowed 
                                        then trx.volumeUp() |> Async.RunSynchronously
                                             do! readAudio (levelDwCounter - 1) (levelUpCounter + 1) envelopeCur
                                        else do! readAudio levelDwCounter levelUpCounter envelopeCur
                    | _ ->              printNext "Ok"
                                        do! readAudio levelDwCounter levelUpCounter envelopeCur
                with ex -> 
                    printfn "Exception during signal processing: %A" ex.Message
                    do! readAudio levelDwCounter levelUpCounter envelopeCur
            }

        let rec processAudio irCounter envelopePrev =
            async{
                Thread.Sleep timer
            
                let envelopeCur = int <| adc.ReadAddressByte audioEnvelopeChannel

                envelopeLed.SoftPwmValue <- int (calcDutyCycle (float envelopeCur))

                let printNext text =

                    if Global.PrintAsyncOutput then
                        if envelopeCur <> envelopePrev
                        then printf "\n%s : %d " text envelopeCur
                        else printf "."

                    if Global.PrintAsyncAllLectures then
                        printf "\n%s : %2d\t %d" text envelopeCur irCounter

                try
                    match envelopeCur with
                    | x when x > Global.AudioProfile.SoundIdealUpperLimit -> 
                                        if irCounter > (Global.AudioProfile.MaxIRDecreasesAllowed * -1)
                                        then printNext "Up>"
                                             trx.volumeDown() |> Async.RunSynchronously
                                             do! processAudio (irCounter - 1) envelopeCur
                                        else printNext "Up|"
                                             do! processAudio irCounter envelopeCur
                    | x when x < Global.AudioProfile.SoundIdealBottomLimit && x > lowVolumeLevelConsideredMute -> 
                                        if irCounter < Global.AudioProfile.MaxIRIncreasesAllowed 
                                        then printNext "Dw>"
                                             trx.volumeUp() |> Async.RunSynchronously
                                             do! processAudio (irCounter + 1) envelopeCur
                                        else printNext "Dw|"
                                             do! processAudio irCounter envelopeCur
                    | _ ->              printNext "Ok|"
                                        do! processAudio irCounter envelopeCur
                with ex -> 
                    printfn "Exception during signal processing: %A" ex.Message
                    do! processAudio 0 envelopeCur
            }

        member __.run () =
            try
                printfn ""
                printfn "Set device to recommended initial volume: %d" Global.AudioProfile.DeviceIdealInitialAudioLevel
                printfn "Press Enter to start..."
                Console.ReadLine() |> ignore
                printfn ""
                printfn "Process started"

                let rec printMenuAndReadKey () =
                    printfn "\n"
                    printfn "Profile: %s" Global.AudioProfile.Name
                    printfn "Option                                     Update keys             Current value"
                    printfn "--------------------------------------------------------------------"
                    printfn "Show on/off audio read values              [P]                     %b" Global.PrintAsyncOutput
                    printfn "Show on/off audio read values detailed     [L]                     %b" Global.PrintAsyncAllLectures
                    printfn "Stop                                       [X]"
                    printfn "Audio sensor from device -------------------------------------------------------"
                    printfn "Upper noise limit                          [Up/Down Arrows]        %d" Global.AudioProfile.SoundIdealUpperLimit
                    printfn "Lower noise limit                          [Left/Right Arrows]     %d" Global.AudioProfile.SoundIdealBottomLimit
                    printfn "IR signals to device ------------------------------------------------------"
                    printfn "Max volume IR increases                    [W/S Arrows]            %d" Global.AudioProfile.MaxIRIncreasesAllowed
                    printfn "Min volume IR decreases                    [A/S Arrows]            %d" Global.AudioProfile.MaxIRDecreasesAllowed
                    printfn "--------------------------------------------------------------------"
                    printf "Select an option: "

                    let keyInfo = Console.ReadKey(true)
                    printfn "%c" keyInfo.KeyChar

                    let profile = Global.AudioProfile

                    match keyInfo.Key with
                    // General values
                    | ConsoleKey.X              -> () // Exit
                    | ConsoleKey.P              -> Global.PrintAsyncOutput      <- Global.PrintAsyncOutput |> not;      Global.PrintAsyncAllLectures <- false;  printMenuAndReadKey()
                    | ConsoleKey.L              -> Global.PrintAsyncAllLectures <- Global.PrintAsyncAllLectures |> not; Global.PrintAsyncOutput      <- false;  printMenuAndReadKey()
                    // Sound detector levels
                    | ConsoleKey.UpArrow        -> Global.AudioProfile <- { profile with SoundIdealUpperLimit = profile.SoundIdealUpperLimit + 1 };             printMenuAndReadKey()
                    | ConsoleKey.DownArrow      -> Global.AudioProfile <- { profile with SoundIdealUpperLimit = profile.SoundIdealUpperLimit - 1 };             printMenuAndReadKey()
                    | ConsoleKey.RightArrow     -> Global.AudioProfile <- { profile with SoundIdealBottomLimit = profile.SoundIdealBottomLimit + 1 };           printMenuAndReadKey()
                    | ConsoleKey.LeftArrow      -> Global.AudioProfile <- { profile with SoundIdealBottomLimit = profile.SoundIdealBottomLimit - 1 };           printMenuAndReadKey()
                    // Device audio levels
                    | ConsoleKey.W              -> Global.AudioProfile <- { profile with MaxIRIncreasesAllowed = profile.MaxIRIncreasesAllowed + 1 };           printMenuAndReadKey()
                    | ConsoleKey.S              -> Global.AudioProfile <- { profile with MaxIRIncreasesAllowed = profile.MaxIRIncreasesAllowed - 1 };           printMenuAndReadKey()
                    | ConsoleKey.D              -> Global.AudioProfile <- { profile with MaxIRDecreasesAllowed = profile.MaxIRDecreasesAllowed + 1 };           printMenuAndReadKey()
                    | ConsoleKey.A              -> Global.AudioProfile <- { profile with MaxIRDecreasesAllowed = profile.MaxIRDecreasesAllowed - 1 };           printMenuAndReadKey()
                    // Default value
                    | _                         -> printfn "Invalid option";                                                                                    printMenuAndReadKey()

                use cancellationSource = new CancellationTokenSource()
                //Async.Start((readAudio 0 0 -1), cancellationSource.Token)
                Async.Start((processAudio 0 0), cancellationSource.Token)

                printMenuAndReadKey()
                cancellationSource.Cancel()

                printfn ""
                printfn "Turning off sensors"
                Thread.Sleep(1000)
                OutputLed().Off()

            with ex -> printfn "%A" ex
                       destroy()