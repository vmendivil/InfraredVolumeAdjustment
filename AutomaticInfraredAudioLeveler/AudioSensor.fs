﻿namespace Vhmc.Pi.I2C

open Unosquare.RaspberryIO
open Unosquare.WiringPi
open Unosquare.RaspberryIO.Abstractions
open System
open System.Threading

[<AutoOpen>]
module private AudioSensorHelpers =
    // Pwm
    let pwmMinRange = 0
    let pwmMaxRange = 100

    // i2c configuration
    let accuracy = 255 // 8 bits
    //let refVoltage = 0.33

    // Audio levels
    let ideal = 5
    let above = ideal + 2
    let below = ideal - 0
        
    // Functions
    let calcDutyCycle analogValue = (analogValue * float pwmMaxRange) / float accuracy

module AudioSensor =

    type AudioSensor() =

        do
            Pi.Init<BootstrapWiringPi>()

        // Configure I2C devices
        let adcAddress = 0x48 // run i2cdetect -y 1
        let adc = Pi.I2C.AddDevice(adcAddress)
        let envCh = 0 // Envelope channel

        // Pin setup
        let envelopeLed = (Pi.Gpio.[BcmPin.Gpio05]) :?> GpioPin // Led to visualize the envelope input from the audio sensor
        do 
            envelopeLed.PinMode <- GpioPinDriveMode.Output
            envelopeLed.StartSoftPwm(pwmMinRange, pwmMaxRange)

        //let irIn =(Pi.Gpio.[BcmPin.Gpio17]) :?> GpioPin // IR input
        //let irOut =(Pi.Gpio.[BcmPin.Gpio18]) :?> GpioPin // IR output

        let destroy () =
            printf "Destroyed..."
            Thread.Sleep(1)
            // Reset pins to default
            envelopeLed.SoftPwmValue <- 0

        do System.Console.CancelKeyPress |> Event.add (fun _ -> destroy()) // Ctrl+C to finish application

        let mutable envelopePrev = -1

        let loop () =
            while true do
                let envelopeCur = int <| adc.ReadAddressByte envCh
        
                match envelopeCur <> envelopePrev with
                | true ->
                        envelopePrev <- envelopeCur
                        envelopeLed.SoftPwmValue <- int (calcDutyCycle (float envelopePrev))
                        printfn ""

                        match envelopePrev with
                        | x when x > above -> printf "Up : %d" x
                        | x when x < below -> printf "Dw : %d" x
                        | x ->                printf "Ok : %d" x
                | _ -> printf "."

                Thread.Sleep 250
        member __.run () =
            try loop()
            with ex -> printfn "%A" ex
                       destroy()