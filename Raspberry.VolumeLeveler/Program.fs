open Vhmc.Pi.Functions
open Vhmc.Pi.Test.TestHelpers
open Vhmc.Pi.Common


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
        printfn "6) Test IR transmitter"
        printfn "7) Profile configuration"
        printfn "8) Test audio sensor"
        printfn "9) Test output led"
        printfn "10) Manual volume control"
        printfn "0) Exit"
        printfn ""
        let option = readConsoleInt "Select an option: "

        try
            match option with
            | -1 -> TestAsync().run() |> fun _ -> run false // TODO: Remove, this is just for testing
            | 0 -> exitApplication ()
            | 1 -> startIRAudioLeveler () |> fun _ -> run false
            | 2 -> createProfile () |> fun _ -> run false
            | 3 -> listProfiles () |> fun _ -> run true
            | 4 -> deleteProfile () |> fun _ -> run false
            | 5 -> updateProfile () |> fun _ -> run false
            | 6 -> testProfile () |> fun _ -> run false
            | 7 -> profileConfiguration () |> fun _ -> run true
            | 8 -> testAudioSensor () |> fun _ -> run false
            | 9 -> testOutputLed () |> fun _ -> run false
            | 10 -> manualVolume () |> fun _ -> run false
            | _ -> invalidOption () |> fun _ -> run false
        with ex ->
                printfn "\nException ocurred: %s\n" ex.Message
                //printfn "StackTrace: %s" ex.StackTrace
                run false

    startDaemon()
    run false
    stopDaemon()

    printfn "Finished"

    0