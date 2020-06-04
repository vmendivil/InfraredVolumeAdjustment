namespace Vhmc.Pi.Common

open Newtonsoft.Json
open System.Diagnostics


[<AutoOpen>]
module PiGpioDaemon =

    let sendCommand command = 
        async{
            Process.Start("sudo", command) |> fun x -> x.WaitForExit()
            return ()
        }
    let startDaemon () = "pigpiod" |> sendCommand |> Async.RunSynchronously // Start pigpio daemon
    let stopDaemon () = "killall pigpiod" |> sendCommand |> Async.RunSynchronously // Stop pigpio daemon
    let profilesFile = sprintf @"IR_AudioProfiles.json"


[<AutoOpen>]
module CommonFunctions =
    
    type ResultBuilder() =
        member __.Return x = Ok x
        member __.Zero() = Ok ()
        member __.Bind(xResult, f) = Result.bind f xResult


[<AutoOpen>]
module Json =

    let serialize obj = JsonConvert.SerializeObject obj
    let deserialize<'a> str =
        try
            JsonConvert.DeserializeObject<'a> str
            |> Result.Ok
        with
            // catch all exceptions and convert to Result
            | ex -> Result.Error ex
