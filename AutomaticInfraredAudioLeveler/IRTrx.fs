namespace Vhmc.Pi.IR

open System.Diagnostics

[<AutoOpen>]
module private IRTrxHelpers =

    // IR Trx pin
    let irTrx = 18
    // Command
    let command pin irConfigFile instruction = sprintf "python irrp.py -p -g%d -f%s %s" pin irConfigFile instruction


module IRTrx =
    type IRInsigniaRokuTV() =
        let irConfig = "InsigniaRokuTV.json"
        let sendIR value = Process.Start("sudo", value) |> fun x -> x.WaitForExit()

        member __.volumeUp ()   = sendIR <| (command irTrx irConfig "VolumeUp")
        member __.volumeDown () = sendIR <| (command irTrx irConfig "VolumeDown")
