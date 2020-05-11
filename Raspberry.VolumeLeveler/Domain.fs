namespace Vhmc.Pi.VolumeLeveler

[<AutoOpen>]
module Domain =

    type AudioProfile =
        {
            Name: string
            IRFileName: string
            IdealAudioLevel: int
            IdealUpperLimit: int
            IdealBottomLimit: int
            MaxIRIncreasesAllowed: int
            MaxIRDecreasesAllowed: int
        }
        with
            static member Empty =
                {
                    Name = ""
                    IRFileName = ""
                    IdealAudioLevel = 0
                    IdealUpperLimit = 0
                    IdealBottomLimit = 0
                    MaxIRIncreasesAllowed = 0
                    MaxIRDecreasesAllowed = 0
                }
            member this.printValues () =
                printfn ""
                printfn "Profile values:"
                printfn "Name: %s" this.Name
                printfn "IRFileName: %s" this.IRFileName
                printfn "IdealAudioLevel: %d" this.IdealAudioLevel
                printfn "IdealUpperLimit: %d" this.IdealUpperLimit
                printfn "IdealBottomLimit: %d" this.IdealBottomLimit
                printfn "MaxIRIncreasesAllowed: %d" this.MaxIRIncreasesAllowed
                printfn "MaxIRDecreasesAllowed: %d" this.MaxIRDecreasesAllowed
                printfn ""

    type AudioProfiles =
        {
            Profiles: AudioProfile list
        }
        with
            static member Empty =
                {
                    Profiles = []
                }
            static member Init profiles =
                {
                    Profiles = profiles
                }
            
                