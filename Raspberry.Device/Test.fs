namespace Vhmc.Pi.Test

open Vhmc.Pi.Common
open System.Threading
open System
open Vhmc.Pi.Types

// TODO: Remove this file since is just for testing

module TestHelpers =

    type TestAsync () =

        let destroy () =
            printfn "Destroyed..."

        do 
            System.Console.CancelKeyPress |> Event.add (fun _ -> destroy()) // Ctrl+C to finish application

        let rec readAudio value =
            async{

                let printNext () =
                    if Global.PrintAsyncOutput
                    then printf "\n> : %d " value
                    else ()

                printNext()

                Thread.Sleep(1000)

                do! readAudio (value + 1)
            }

        let rec readKey () =
            let keyInfo = Console.ReadKey(true)

            match keyInfo.Key with
            | ConsoleKey.P -> printfn "\nResume/Pause output"
                              Global.PrintAsyncOutput <- Global.PrintAsyncOutput |> not
                              readKey() 
            | ConsoleKey.UpArrow -> printfn "\nUpper volume Up"; readKey()
            | ConsoleKey.DownArrow -> printfn "\nUpper volume Down"; readKey()
            | ConsoleKey.RightArrow -> printfn "\Lower volume Down"; readKey()
            | ConsoleKey.LeftArrow -> printfn "\Lower volume Down"; readKey()
            | ConsoleKey.X -> () // Exit
            | _ -> printfn "\nInvalid option"; readKey()

        member __.run () =
            try 
                use cancellationSource = new CancellationTokenSource()
                Async.Start((readAudio -1), cancellationSource.Token)

                readKey()
                cancellationSource.Cancel()

            with ex -> printfn "%A" ex
                       destroy()

