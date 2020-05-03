open Vhmc.Pi.I2C.AudioSensor
open Vhmc.Pi.IR.IRTrx


[<EntryPoint>]
let main argv =

    let programToRun =
        try int (argv.[0])
        with _ -> 0

    match programToRun with
    //| 111 -> Program that will combine audio sensor with volume commands
    | 222 -> AudioSensor().run()
    | 333 -> IRInsigniaRokuTV().volumeUp()
    | 444 -> IRInsigniaRokuTV().volumeDown()
    | _ -> printfn "Option not valid: %d" programToRun

    programToRun
