open Vhmc.Pi.I2C.AudioSensor
open Vhmc.Pi.VolumeLeveler.VolumeLeveler
open Vhmc.Pi.IR


[<EntryPoint>]
let main argv =

    let programToRun =
        try int (argv.[0])
        with _ -> 0

    let idealVolume =
        let ideal = 5
        if argv.Length > 1
        then
            try
                argv.[1] |> int
            with | _ -> ideal
        else ideal

    match programToRun with
    | 111 -> AudioLeveler(idealVolume).run()
    | 222 -> AudioSensor().run()
    | 333 -> IRTrx.IRInsigniaRokuTV().volumeUp()
    | 444 -> IRTrx.IRInsigniaRokuTV().volumeDown()
    | _ -> printfn "Option not valid: %d" programToRun

    programToRun
