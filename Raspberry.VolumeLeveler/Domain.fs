namespace Vhmc.Pi.VolumeLeveler.Domain

[<AutoOpen>]
module Domain =

    type AudioProfile =
        {
            Name: string
            IRFileName: string
            DeviceIdealInitialAudioLevel: int
            SoundIdealUpperLimit: int
            SoundIdealBottomLimit: int
            MaxIRIncreasesAllowed: int
            MaxIRDecreasesAllowed: int
        }
        with
            static member Empty =
                {
                    Name = ""
                    IRFileName = ""
                    DeviceIdealInitialAudioLevel = 0
                    SoundIdealUpperLimit = 0
                    SoundIdealBottomLimit = 0
                    MaxIRIncreasesAllowed = 0
                    MaxIRDecreasesAllowed = 0
                }
            member this.printValues () =
                printfn ""
                printfn "Profile values:"
                printfn "Name: %s" this.Name
                printfn "IRFileName: %s" this.IRFileName
                printfn "DeviceIdealInitialAudioLevel: %d" this.DeviceIdealInitialAudioLevel
                printfn "SoundIdealUpperLimit: %d" this.SoundIdealUpperLimit
                printfn "SoundIdealBottomLimit: %d" this.SoundIdealBottomLimit
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
            
                