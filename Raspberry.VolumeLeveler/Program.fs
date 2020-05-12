open Vhmc.Pi.VolumeLeveler.Functions


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
        printfn "5) Update profile"
        printfn "6) Test profile"
        printfn "7) Profile configuration"
        printfn "8) Test audio sensor"
        printfn "0) Exit"
        printfn ""
        let option = readConsoleInt "Select an option: "

        try
            match option with
            | 0 -> exitApplication ()
            | 1 -> startIRAudioLeveler () |> fun _ -> run true
            | 2 -> createProfile () |> fun _ -> run false
            | 3 -> listProfiles () |> fun _ -> run true
            | 4 -> deleteProfile () |> fun _ -> run false
            | 5 -> updateProfile () |> fun _ -> run false
            | 6 -> testProfile () |> fun _ -> run false
            | 7 -> profileConfiguration () |> fun _ -> run true
            | 8 -> testAudioSensor () |> fun _ -> run false
            | _ -> invalidOption () |> fun _ -> run false
        with ex ->
                printfn "Exception ocurred: %s" ex.Message
                run false

    run false

    printfn "Finished"

    0