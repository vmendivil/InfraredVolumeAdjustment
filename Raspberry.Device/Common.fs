namespace Vhmc.Pi.Common

open Newtonsoft.Json


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
