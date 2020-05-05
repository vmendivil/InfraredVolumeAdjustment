namespace Vhmc.Pi.VolumeLeveler

open Unosquare.RaspberryIO
open Unosquare.WiringPi
open Unosquare.RaspberryIO.Abstractions
open System.Threading
open System.Diagnostics


module VolumeLeveler =

    type IRInsigniaRokuTV() =

        // IR Trx pin
        let irTrx = (int) BcmPin.Gpio18

        // Command
        let irCommand pin irConfigFile instruction = sprintf "python irrp.py -p -g%d -f%s %s" pin irConfigFile instruction

        let irConfig = "InsigniaRokuTV.json"
        let sendCommand value = Process.Start("sudo", value) |> fun x -> x.WaitForExit()

        let up () = 
            sendCommand <| (irCommand irTrx irConfig "VolumeUp")
            printf ">"
        let down () = 
            sendCommand <| (irCommand irTrx irConfig "VolumeDown")
            printf "<"

        do 
            sendCommand <| ("pigpiod")
        
        member __.volumeUp ()   = 
            async{
                up()
                return ()
            }
        member __.volumeDown () = 
            async{
                down()
                return ()
            }

        member __.stopService () = sendCommand <| ("killall pigpiod")


    type AudioLeveler (idealVolume) =

        // Read intervals
        let timer = 250 // ms

        // Pwm
        let pwmMinRange = 0
        let pwmMaxRange = 100

        // i2c configuration
        let accuracy = 255 // 8 bits
        //let refVoltage = 0.33

        // Audio levels
        let above = idealVolume + 2
        let below = idealVolume - 2
            
        // Functions
        let calcDutyCycle analogValue = (analogValue * float pwmMaxRange) / float accuracy

        let device = IRInsigniaRokuTV()

        do
            Pi.Init<BootstrapWiringPi>()

        do 
            printfn "Volume Leveler"
            printfn "Ideal Volume: %d" idealVolume
            printfn "Above: %d" above
            printfn "Below: %d" below
            printfn "Samples/second: %d" (1000 / timer)
            printfn ""

        // Configure I2C devices
        let adcAddress = 0x48 // run i2cdetect -y 1
        let adc = Pi.I2C.AddDevice(adcAddress)
        let envCh = 0 // Envelope channel

        // Led to visualize the envelope input from the audio sensor
        let envelopeLed = 
            let led =(Pi.Gpio.[BcmPin.Gpio05]) :?> GpioPin
            led.PinMode <- GpioPinDriveMode.Output
            led.StartSoftPwm(pwmMinRange, pwmMaxRange)
            led

        let destroy () =
            printf "Destroyed..."
            Thread.Sleep(1)
            // Stop services and reset pin values
            device.stopService()
            envelopeLed.SoftPwmValue <- 0

        do System.Console.CancelKeyPress |> Event.add (fun _ -> destroy()) // Ctrl+C to finish application

        let rec readAudio envelopePrev =
            Thread.Sleep timer
            let envelopeCur = int <| adc.ReadAddressByte envCh

            envelopeLed.SoftPwmValue <- int (calcDutyCycle (float envelopeCur))

            let printNext text =
                if envelopeCur <> envelopePrev
                then printf "\n%s : %d " text envelopeCur
                else printf "."

            match envelopeCur with
            | x when x > above -> 
                                printNext "Up"
                                device.volumeDown() |> Async.RunSynchronously
                                readAudio envelopeCur
            | x when x < below -> 
                                printNext "Dw"
                                device.volumeUp() |> Async.RunSynchronously
                                readAudio envelopeCur
            | x -> 
                                printNext "Ok"
                                readAudio envelopeCur

        member __.run () =
            try readAudio -1
            with ex -> printfn "%A" ex
                       destroy()