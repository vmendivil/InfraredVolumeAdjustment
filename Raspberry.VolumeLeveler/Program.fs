open Vhmc.Pi.VolumeLeveler.VolumeLeveler


[<EntryPoint>]
let main argv =

    let idealVolume =
        let ideal = 5
        if argv.Length > 1
        then
            try
                argv.[0] |> int
            with | _ -> ideal
        else ideal

    AudioLeveler(idealVolume).run()

    0
