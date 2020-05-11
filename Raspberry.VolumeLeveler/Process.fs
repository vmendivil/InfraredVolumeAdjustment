namespace Vhmc.Pi.VolumeLeveler

open Unosquare.RaspberryIO.Abstractions
open System.Diagnostics
open System
open System.IO
open System.Configuration
open Unosquare.RaspberryIO
open Unosquare.WiringPi
open System.Threading


[<AutoOpen>]
module private ProcessHelpers =

    let sendCommand command = 
        async{
            Process.Start("sudo", command) |> fun x -> x.WaitForExit()
            return ()
        }
    let startDaemon () = "pigpiod" |> sendCommand |> Async.RunSynchronously // Start pigpio daemon
    let stopDaemon () = "killall pigpiod" |> sendCommand |> Async.RunSynchronously // Stop pigpio daemon

    let profilesFile = sprintf @"IRAudioProfiles.json"


[<AutoOpen>]
module Process =

    type IRTrxCommands (irCommandsFile) =
        let irTrxPin = (int) BcmPin.Gpio18
        let irCommand pin irConfigFile instruction = sprintf "python irrp.py -p -g%d -f%s %s" pin irConfigFile instruction

        do startDaemon()
        
        member __.volumeUp () = sendCommand <| (irCommand irTrxPin irCommandsFile "VolumeUp")
        member __.volumeDown () = sendCommand <| (irCommand irTrxPin irCommandsFile "VolumeDown")

        interface IDisposable with
            member __.Dispose () = stopDaemon()

    type IRRecCommands (profileName: string) =
        let irRecPin = (int) BcmPin.Gpio17
        let irCommand pin irConfigFile = sprintf "python irrp.py -r -g%d -f%s VolumeUp VolumeDown" pin irConfigFile

        member __.startRecording () =
            let outputFile = sprintf "%s" profileName
            startDaemon()
            sendCommand <| irCommand irRecPin outputFile |> Async.RunSynchronously
            stopDaemon()

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
            | _ -> Error "ProfileNotFound"

        member this.createProfile (profile: AudioProfile) =
            match this.existProfile profile.Name with
            | false -> 
                    loadAudioProfiles()
                    |> fun x -> { x with Profiles = x.Profiles @ [profile] }
                    |> saveProfiles
                    |> Ok
            | true -> Error "ProfileAlreadyExists"

        member __.getProfile profileName =
            match loadAudioProfiles().Profiles |> List.tryFind (fun x -> x.Name = profileName) with
            | Some x -> Ok x
            | None -> Error "ProfileNotFound"

        member __.getAudioProfiles () = loadAudioProfiles()

    type AudioSensor () =

        let timer = 200
        let pwmMinRange = 0
        let pwmMaxRange = 100
        let accuracy = 255 
        let calcDutyCycle analogValue = (analogValue * float pwmMaxRange) / float accuracy

        do
            Pi.Init<BootstrapWiringPi>()

        let adcAddress = 0x48
        let adc = Pi.I2C.AddDevice(adcAddress)
        let envCh = 0

        let envelopeLed = 
            let led =(Pi.Gpio.[BcmPin.Gpio05]) :?> GpioPin
            led.PinMode <- GpioPinDriveMode.Output
            led.StartSoftPwm(pwmMinRange, pwmMaxRange)
            led

        let destroy () =
            printf "Destroyed..."
            Thread.Sleep(1)
            envelopeLed.SoftPwmValue <- 0

        do System.Console.CancelKeyPress |> Event.add (fun _ -> destroy()) // Ctrl+C to finish application

        let rec readAudio envelopePrev =
            Thread.Sleep timer
            
            let envelopeCur = int <| adc.ReadAddressByte envCh
            envelopeLed.SoftPwmValue <- int (calcDutyCycle (float envelopeCur))

            let printNext text =
                if envelopeCur <> envelopePrev
                then printf "\n%s : %d " text envelopeCur
                else printf "."

            printNext ">"
            readAudio envelopeCur

        member __.run () =
            try readAudio -1
            with ex -> printfn "%A" ex
                       destroy()

    type IrAudioLeveler (profile: AudioProfile) =

        let timer = (int) ConfigurationManager.AppSettings.["AudioLectureSampleMS"] // Read intervals

        do
            Pi.Init<BootstrapWiringPi>()

        do 
            printfn "Configuration:"
            printfn "Samples/second: %d" (1000 / timer)
            profile.printValues()

        // Pwm
        let pwmMinRange = 0
        let pwmMaxRange = 100

        // i2c configuration
        let accuracy = 255 // 8 bits
            
        // Functions
        let calcDutyCycle analogValue = (analogValue * float pwmMaxRange) / float accuracy

        // Configure I2C devices
        let adcAddress = (int) ConfigurationManager.AppSettings.["AdcAddress"] // run i2cdetect -y 1
        let adc = Pi.I2C.AddDevice(adcAddress)
        let audioEnvelopeChannel = 0 // Envelope channel

        // Led to visualize the envelope input from the audio sensor
        let envelopeLed = 
            let led =(Pi.Gpio.[BcmPin.Gpio05]) :?> GpioPin
            led.PinMode <- GpioPinDriveMode.Output
            led.StartSoftPwm(pwmMinRange, pwmMaxRange)
            led

        let trx = new IRTrxCommands(profile.IRFileName)

        let destroy () =
            printf "Destroyed..."
            Thread.Sleep(1)
            trx :> IDisposable |> fun x -> x.Dispose()
            envelopeLed.SoftPwmValue <- 0

        do System.Console.CancelKeyPress |> Event.add (fun _ -> destroy()) // Ctrl+C to finish application

        let rec readAudio levelDwCounter levelUpCounter envelopePrev =
            Thread.Sleep timer
            
            let envelopeCur = int <| adc.ReadAddressByte audioEnvelopeChannel

            envelopeLed.SoftPwmValue <- int (calcDutyCycle (float envelopeCur))

            let printNext text =
                if envelopeCur <> envelopePrev
                then printf "\n%s : %d " text envelopeCur
                else printf "."

            match envelopeCur with
            | x when x > profile.IdealUpperLimit -> 
                                printNext "Up"
                                if levelDwCounter < profile.MaxIRDecreasesAllowed
                                then
                                    trx.volumeDown() |> Async.RunSynchronously
                                    readAudio (levelDwCounter + 1) (levelUpCounter - 1) envelopeCur
                                else
                                    readAudio levelDwCounter levelUpCounter envelopeCur
            | x when x < profile.IdealBottomLimit -> 
                                printNext "Dw"
                                if levelUpCounter < profile.MaxIRIncreasesAllowed 
                                then 
                                    trx.volumeUp() |> Async.RunSynchronously
                                    readAudio (levelDwCounter - 1) (levelUpCounter + 1) envelopeCur
                                else
                                    readAudio levelDwCounter levelUpCounter envelopeCur
            | x -> 
                                printNext "Ok"
                                readAudio levelDwCounter levelUpCounter envelopeCur

        member __.run () =
            try readAudio 0 0 -1
            with ex -> printfn "%A" ex
                       destroy()