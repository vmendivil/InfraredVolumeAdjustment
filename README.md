### BOM

* (1) Raspberry PI 4
* (1) IR Transmitter
  * https://www.digikey.com/product-detail/en/everlight-electronics-co-ltd/IR333C-H0-L10/1080-1082-ND/2675573
* (1) IR Receiver
  * https://www.digikey.com/product-detail/en/vishay-semiconductor-opto-division/TSOP38238/751-1227-ND/1681362
* (1) Analog-Digital Converter PCF8591
  * https://www.nxp.com/docs/en/data-sheet/PCF8591.pdf
* (1) Led
* (3) Resistance 10k
* (2) Resistance 220k
* (1) Transistory NPN
  * https://www.digikey.com/product-detail/en/on-semiconductor/PN2222ATF/PN2222ATFCT-ND/3504402
* (1) Audio Detector
  * https://www.digikey.com/product-detail/en/sparkfun-electronics/SEN-14262/1568-1721-ND/7725299

### Circuit Diagram

Circuit diagram source file is withing this repository. Frizting is the software used for its design.

#### Analog-Digital Converter

Diagram must use ADC PCF8591, however, fritzing didn't have that specific chip, so, for drawing purposes, a different chip with the same number of pins was used.

#### Sound Detector

The diagram represents an audio input. Current implementation uses Sparkfun Sound Detector and uses Envelope pin output to determine the sound level.

### Configure I2C

The i2c interface in raspberry is closed by default, it has to be open manually.

Steps:

	1) Run command: sudo raspi-config
	2) Follow path:
		a. 5 Interfacing Options
		b. P5 I2C
		c. Yes
		d. Finish
	3) Restart Raspberry

To validate i2c module is started:

	• Command: lsmod | grep i2c

Install i2c-Tools

	• Command: sudo apt-get install i2c-tools

Detect device addresses (which will be used in the program):

	• Command: i2cdetect -y 1



### Functions

	1) Record IR signals from remote controller.
	2) Send IR signals from raspberry and IR transmitter to device.
	3) Read and process data from sound detector
	4) Define rules about how program will work to process sound levels from device and send IR signals to device.
	5) Program to level audio levels.

### Code and Libraries

This program uses F# and Python for different purposes.

F# is used as the main programing language to drive the logic that will read the sound level and will trigger the instructions to increase/decrease the volume level.

Python is used to have quick and easy access to pigpio library. This library provides an example on how to record and reproduce IR signals.

F# uses Unosquare.RaspberryIO NuGet package. Python uses pigpio library.

### Install pigpio in Raspberry

Follow instructions: http://abyz.me.uk/rpi/pigpio/download.html

### How to record IR signals

	1) Connect the IR receiver to Raspberry.
	2) Download python pigpio script to record and reproduce IR signals.
		a. Link: http://abyz.me.uk/rpi/pigpio/examples.html#Python%20code
			i. IR Record and Playback: http://abyz.me.uk/rpi/pigpio/code/irrp_py.zip
	3) Start pigpio daemon: sudo pigpiod
	4) To record the GPIO connected to the IR receiver, a file for the recorded codes, and the codes to be recorded are given.
		a. Command: sudo python irrp.py -r -g4 -fir-codes vol+ vol- 1 2 3 4 5 6 7 8 9 0
	5) Recorded signals get saved in a json file format
	6) Stop pigpio daemon: sudo killall pigpiod

To get help on available commands and options, run command: sudo python irrp.py -h

### How to playback IR signals

	1) Connect the IR transmitter to Raspberry
	2) Download python pigpio script to record and reproduce IR signals.
		a. Link: http://abyz.me.uk/rpi/pigpio/examples.html#Python%20code
			i. IR Record and Playback: http://abyz.me.uk/rpi/pigpio/code/irrp_py.zip
	3) Start pigpio daemon: sudo pigpiod
	4) To playback the GPIO connected to the IR transmitter, the file containing the recorded codes, and the codes to be played back are given. 
		a. Command: sudo python irrp.py -p -g18 -fir-codes 2 3 4
	5) Stop pigpio daemon: sudo killall pigpiod

### Read data from Sound Detector

Sound detector functionality is explanied in the below link. We are using the Envelope output.

https://learn.sparkfun.com/tutorials/sound-detector-hookup-guide

### Rules on how the program Volume Leveler program should work

	1) Program should be written in F#, why? Just for fun.
	2) Program should call functions inside python script to send IR signals.
	3) A range should be specified where audio level is acceptable.
		a. If above that range, reduce the volume.
		b. If below that range, increase the volume.
	4) A maximum and minimum amount of allowed IR signals to increase or decrease the volume must be set to prevent the program going below/above those values.
	5) Should be easy to define what IR config file will be used to generate the IR signals.
	6) Program should read audio levels at a configured rate per second.

Program is kept as simple as possible, initially only focused on doing the job: reading audio, defining its level and increase/reduce audio levels by sending IR signals.

### How to run the program

The program follows some assumptions:

	a) You have python installed
	b) You have pigpio python package installed
	c) You have dotNet installed
	d) You can properly ssh to your Raspberry

Deploy app:

	1) Clone repo
	2) Open PowerShell and run Deploy.ps1 script
		a. Script assumes you can ssh to your Raspberry.
		b. $SshPrivateKey must be the path to your private key
		c. In lines 8 and 9, update the IP and the path where you will be deploying your code

Run the app:

	1) Ssh to your raspberry and navigate to the folder where the code was deployed
	2) Run program: dotnet AutomaticInfraredAudioLeveler.dll <option to run>
		a. Audio leveler: dotnet AutomaticInfraredAudioLeveler.dll 111 15
		b. Audio sensor: dotnet AutomaticInfraredAudioLeveler.dll 222
		c. Volume up: dotnet AutomaticInfraredAudioLeveler.dll 333
		d. Volume down: dotnet AutomaticInfraredAudioLeveler.dll 444

The app have different actions:

	• Command: dotnet AutomaticInfraredAudioLeveler.dll <program to run> <ideal volume>

```F#
	    match programToRun with
	    | 111 -> AudioLeveler(idealVolume).run()
	    | 222 -> AudioSensor().run()
	    | 333 -> IRTrx.IRInsigniaRokuTV().volumeUp()
	    | 444 -> IRTrx.IRInsigniaRokuTV().volumeDown()
	    | _ -> printfn "Option not valid: %d" programToRun
```
