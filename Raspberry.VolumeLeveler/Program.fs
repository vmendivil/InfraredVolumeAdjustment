open System
open Vhmc.Pi.VolumeLeveler


[<AutoOpen>]
module private ProgramHelpers =

    let readConsole message =
        printf "%s" message
        Console.ReadLine()

    let profiles = Profiles()

    let startIRAudioLeveler () =
        printfn "\nAudio leveler"
        let profileName = readConsole "Profile name: "
        
        let profile =
            match profiles.getProfile profileName with
            | Ok profile -> profile
            | Error x -> failwithf "%s" x

        readConsole "Press Enter to start. Use Ctrl+C to stop process" |> ignore
        IrAudioLeveler(profile).run()
        printfn "Process stoped"
    
    let createProfile () =
        printfn "\nCreate profile"
        let name = readConsole "Name: "
        let irFileName = sprintf "IR_%s.json" name
        let idealAudioLevel = readConsole "Ideal audio level: "
        let idealUpperLimit = readConsole "Ideal upper limit: "
        let idealBottomLimit = readConsole "Ideal bottom limit: "
        let maxIRIncreasesAllowed = readConsole "Max IR volume increases allowed: "
        let maxIRDecreasesAllowed = readConsole "Max IR volume decreases allowed: "

        printfn "\nTake your IR remote controller and follow instructions to record IR signals."
        readConsole "Press Enter when you are ready to start recording..." |> ignore

        IRRecCommands(irFileName).startRecording()

        let create () =
            {
                Name = name
                IRFileName = irFileName
                IdealAudioLevel = int idealAudioLevel
                IdealUpperLimit = int idealUpperLimit
                IdealBottomLimit = int idealBottomLimit
                MaxIRIncreasesAllowed = int maxIRIncreasesAllowed
                MaxIRDecreasesAllowed = int maxIRDecreasesAllowed
            } |> profiles.createProfile

        match create() with
        | Ok _ -> printfn "Profile %s created"
        | Error x -> failwithf "FailedToCreateProfile:\n%s" x
    
    let listProfiles () =
        let profiles = profiles.getAudioProfiles().Profiles
        printfn "\nProfiles"
        printfn "Count: %d" profiles.Length
        printfn "-----"
        profiles |> List.iter (fun x -> printfn "%s" x.Name)
        printfn "-----"

    let deleteProfile () =
        printfn "\nDelete profile"
        let name = readConsole "Name: "
        match profiles.removeProfile name with
        | Ok _ -> printfn "Profile deleted"
        | Error x -> failwithf "%s" x

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

        printfn "Test finished"

    let profileConfiguration () =
        printfn "\nProfile configuration"
        let profileName = readConsole "Profile name: "
        
        let profile =
            match profiles.getProfile profileName with
            | Ok profile -> profile
            | Error x -> failwithf "%s" x

        profile.printValues()

    let testAudioSensor () =
        printfn "\nProfile configuration"

        readConsole "Press Enter to start. Use Ctrl+C to stop process" |> ignore
        AudioSensor().run()
        printfn "Process stoped"


    let invalidOption () = printfn "Option not  valid"  

    let exitApplication () = printfn "Exiting..."


[<EntryPoint>]
let main argv =

    let rec run waitForKeyPress =

        if waitForKeyPress then readConsole "\nPress Enter to continue" |> ignore

        printfn ""
        printfn "----------------"
        printfn "IR Audio Leveler"
        printfn "----------------"
        printfn "Menu:"
        printfn ""
        printfn "1) Start IR Audio Leveler"
        printfn "2) Create new profile"
        printfn "3) List all profiles"
        printfn "4) Delete profile"
        printfn "5) Test profile"
        printfn "6) Profile configuration"
        printfn "7) Test audio sensor"
        printfn "0) Exit"
        printf "\nSelect an option: "

        let option = Console.ReadLine()

        try
            match option |> int with
            | 0 -> exitApplication ()
            | 1 -> startIRAudioLeveler () |> fun _ -> run true
            | 2 -> createProfile () |> fun _ -> run true
            | 3 -> listProfiles () |> fun _ -> run true
            | 4 -> deleteProfile () |> fun _ -> run true
            | 5 -> testProfile () |> fun _ -> run true
            | 6 -> profileConfiguration () |> fun _ -> run true
            | 7 -> testAudioSensor () |> fun _ -> run true
            | _ -> invalidOption () |> fun _ -> run true
        with ex ->
                printfn "Exception ocurred: %s" ex.Message
                run true

    run false

    0