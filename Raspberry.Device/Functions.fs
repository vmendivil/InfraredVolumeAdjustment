namespace Vhmc.Pi.Functions

open System
open Vhmc.Pi.Domain
open Vhmc.Pi.Types


[<AutoOpen>]
module private FunctionsHelpers =

    let profiles = Profiles()

    let getOrFailProfile profileName =
        match profiles.getProfile profileName with
        | Ok profile -> profile
        | Error x -> failwithf "%s" x

    let deleteOrFailProfile profile =
        match profiles.removeProfile profile with
        | Ok _ -> printfn "Profile deleted"
        | Error x -> failwithf "%s" x

    let createOrFailProfile profile =
        match profiles.createProfile profile with
        | Ok _ -> printfn "\nProfile %s created" profile.Name
        | Error x -> failwithf "FailedToCreateProfile:\n%s" x

    let updateOrFailProfile profile =
        match profiles.updateProfile profile with
        | Ok _ -> printfn "Profile updated"
        | Error x -> failwithf "%s" x

    let buildIRFileName profileName = sprintf "IR_%s.json" profileName


[<AutoOpen>]
module Functions =

    let readConsole message =
        printf "%s" message
        Console.ReadLine()

    let rec readConsoleInt message =
        try readConsole message |> int
        with _ -> readConsoleInt "Invalid option, try again, use only numbers: "
            

    let startIRAudioLeveler () =
        printfn "\nAudio leveler"
        let profileName = readConsole "Profile name: "
        let profile = getOrFailProfile profileName

        printfn ""
        printfn "Starting process" |> ignore
        IrAudioLeveler(profile).run()
        printfn "Process stopped"
    
    let createProfile () =
        printfn "\nCreate profile"
        printfn ""
        let profileName = readConsole "Profile name: "
        let irFileName = buildIRFileName profileName
        printfn ""
        printfn ">> Audio sensor configuration"
        let soundIdealUpperLimit = readConsoleInt "Sound ideal upper limit: "
        let soundIdealBottomLimit = readConsoleInt "Sound ideal bottom limit: "
        printfn ""
        printfn ">> Device configuration"
        let deviceIdealInitialAudioLevel = readConsoleInt "Device initial audio level: "
        let maxIRIncreasesAllowed = readConsoleInt "Max IR volume increases allowed: "
        let maxIRDecreasesAllowed = readConsoleInt "Max IR volume decreases allowed: "

        printfn ""
        printfn "Take your IR remote controller and follow instructions to record IR signals."
        readConsole "Press Enter when you are ready to start recording..." |> ignore
        printfn ""

        IRRecCommands(irFileName).startRecording()

        printfn "\nRecord completed"
        
        {
            Name = profileName
            IRFileName = irFileName
            DeviceIdealInitialAudioLevel = deviceIdealInitialAudioLevel
            SoundIdealUpperLimit = soundIdealUpperLimit
            SoundIdealBottomLimit = soundIdealBottomLimit
            MaxIRIncreasesAllowed = maxIRIncreasesAllowed
            MaxIRDecreasesAllowed = maxIRDecreasesAllowed
        } |> createOrFailProfile
    
    let listProfiles () =
        let profiles = profiles.getAudioProfiles().Profiles
        printfn "\nProfiles"
        printfn "Count: %d" profiles.Length
        printfn "-----"
        profiles |> List.iter (fun x -> printfn "%s" x.Name)
        printfn "-----"

    let deleteProfile () =
        printfn "\nDelete profile"
        let profileName = readConsole "Profile name: "
        let profile = getOrFailProfile profileName
        deleteOrFailProfile profile

    let updateProfile () =
        printfn "\nUpdate profile"
        let profileName = readConsole "Profile name: "
        let profile = getOrFailProfile profileName

        printfn ""
        let deviceIdealInitialAudioLevel = readConsoleInt (sprintf "Device ideal initial audio level [%d]: " profile.DeviceIdealInitialAudioLevel)
        let soundIdealUpperLimit = readConsoleInt (sprintf "Sound ideal upper limit [%d]: "profile.SoundIdealUpperLimit)
        let soundIdealBottomLimit = readConsoleInt (sprintf "Sound ideal bottom limit [%d]: " profile.SoundIdealBottomLimit)
        let maxIRIncreasesAllowed = readConsoleInt (sprintf "Max IR volume increases allowed [%d]: " profile.MaxIRIncreasesAllowed)
        let maxIRDecreasesAllowed = readConsoleInt (sprintf "Max IR volume decreases allowed [%d]: " profile.MaxIRDecreasesAllowed)

        {
            profile with DeviceIdealInitialAudioLevel = deviceIdealInitialAudioLevel
                         SoundIdealUpperLimit = soundIdealUpperLimit
                         SoundIdealBottomLimit = soundIdealBottomLimit
                         MaxIRIncreasesAllowed = maxIRIncreasesAllowed
                         MaxIRDecreasesAllowed = maxIRDecreasesAllowed
        } |> updateOrFailProfile

        printfn "\nProfile %s updated" profileName

    let testProfile () =
        printfn "\nTest profile"
        let profileName = readConsole "Profile name: "
        
        let profile =
            match profiles.getProfile profileName with
            | Ok profile -> profile
            | Error x -> failwithf "%s" x
        
        use trx = new IRTrxCommands(profile.IRFileName)

        printfn "\nPoint AudioLeveler IR towards your IR receiver device"
        readConsole "Press Enter to send a VolumeUp signal..." |> ignore
        trx.volumeUp() |> Async.RunSynchronously
        readConsole "Press Enter to send a VolumeDown signal..." |> ignore
        trx.volumeDown() |> Async.RunSynchronously

        printfn "\nTest finished"

    let profileConfiguration () =
        printfn "\nProfile configuration"
        let profileName = readConsole "Profile name: "
        
        let profile =
            match profiles.getProfile profileName with
            | Ok profile -> profile
            | Error x -> failwithf "%s" x

        profile.printValues()

    let testAudioSensor () =
        printfn "\nTest audio sensor"

        readConsole "Press Enter to start. Press any key to stop" |> ignore
        AudioSensor().run()
        printfn "\nProcess stopped"

    let invalidOption () = printfn "Option not valid"  

    let exitApplication () = printfn "Exiting..."

    let testOutputLed () =
        printfn "\nTest output led"

        let led = OutputLed()

        printfn ""
        printfn "IsInSoftPwmMode: %b" led.Led.IsInSoftPwmMode
        printfn "BcmPin: %A" led.Led.BcmPin
        printfn "PhysicalPinNumber: %A" led.Led.PhysicalPinNumber
        printfn "PinMode: %A" led.Led.PinMode
        printfn "SoftPwmRange: %A" led.Led.SoftPwmRange
        printfn "SoftPwmValue: %A" led.Led.SoftPwmValue

        led.Off()
        printfn ""
        readConsole "Press Enter to send an On signal..." |> ignore
        led.On()
        readConsole "Press Enter to send an Half signal..." |> ignore
        led.Half()
        readConsole "Press Enter to send an Off signal..." |> ignore
        led.Off()
        readConsole "Press Enter to blink 5 times" |> ignore
        led.Blink 5

        printfn "\nTest finished"