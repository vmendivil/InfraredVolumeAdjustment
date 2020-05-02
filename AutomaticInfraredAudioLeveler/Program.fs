open Vhmc.Pi.I2C.AudioSensor


[<AutoOpen>]
module Helpers =

    let inline callRun< ^T when ^T : (member run : unit -> unit)> value =
        try (^T : (member run : unit -> unit) value)
        with ex -> failwithf "Error applying %A.Parse function to value: %A >> %A" (typeof< ^T>) value ex


[<EntryPoint>]
let main argv =

    let programToRun =
        try int (argv.[0])
        with _ -> 0

    match programToRun with
    | 111 -> callRun <| AudioSensor()
    | _ -> printfn "Option not valid: %d" programToRun

    programToRun
