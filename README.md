### What this project does?

This project is inteded to record IR signals from a remote controler, specially volume up and volume down signals, then read the volume level from a device and based on preconfigured parameters decide if the volume is above or below of what is an ideal level of sound and based on that send IR signals to the device to increase or decrease the volume in order to try to keep a constant level of sound.

The project can receive a parameter to decide what piece of code to execute which include just reading audio levels, or independantly sending IR volume up/down signals as well as running the application that will merge all the functionality to effectively read sound levels and send IR signals as needed.

### BOM

* (1) Raspberry PI 4
* (1) [IT Transmitter](https://www.digikey.com/product-detail/en/everlight-electronics-co-ltd/IR333C-H0-L10/1080-1082-ND/2675573)
* (1) [IR Receiver](https://www.digikey.com/product-detail/en/vishay-semiconductor-opto-division/TSOP38238/751-1227-ND/1681362)
* (1) [Analog-Digital Converter PCF8591](https://www.nxp.com/docs/en/data-sheet/PCF8591.pdf)
* (1) Led
* (3) Resistance 10k
* (2) Resistance 220k
* (1) [Transistory NPN](https://www.digikey.com/product-detail/en/on-semiconductor/PN2222ATF/PN2222ATFCT-ND/3504402)
* (1) [Audio Detector](https://www.digikey.com/product-detail/en/sparkfun-electronics/SEN-14262/1568-1721-ND/7725299)

### Circuit Diagram

Circuit diagram source file is part of this repository. Frizting is the software tool used for its design.

![Circuit Diagram](https://github.com/vmendivil/InfraredVolumeAdjustment/raw/2d96b543ed73cc4ad8e13aa27e2d2cb04deebc5b/Circuit%20Diagram.jpg)

#### Analog-Digital Converter

Diagram is using a different chip because the drawing software didn't have the chip that was effectively used. 

Correct chip is ADC PCF8591. Correct port mapping for the chip is displayed below. Physical connections in the diagram are correct and match PCF8591 pins.

![PCF8591](https://github.com/vmendivil/InfraredVolumeAdjustment/raw/e492168e3ce7194127b22192b4772eaa521c0d30/ADC%20PCF8591.png)

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

Restart Raspberry.

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

#### Deploy app

	1) Clone repo
	2) Open PowerShell and run Deploy.ps1 script
		a. Script assumes you can ssh to your Raspberry.
		b. $SshPrivateKey must be the path to your private key
		c. In lines 8 and 9, update the IP and the path where you will be deploying your code

#### Run the app

	1) Ssh to your raspberry and navigate to the folder where the code was deployed
	2) Run program: dotnet AutomaticInfraredAudioLeveler.dll
