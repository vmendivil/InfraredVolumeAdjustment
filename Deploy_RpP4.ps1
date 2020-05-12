$SshPrivateKey = "C:\Vhmc\Msi2Pi4\Msi2Pi4"
$PublishPath = ".\bin\Debug\netcoreapp3.1\*"

Set-Location Raspberry.VolumeLeveler
#Remove-Item $PublishPath -r
dotnet build -c Debug
#dotnet publish --force -c Debug
ssh pi@192.168.1.92 -i $SshPrivateKey "sudo rm -r ./Vhmc/App/AutomaticInfraredAudioLeveler/*"
scp -i $SshPrivateKey -r $PublishPath pi@192.168.1.92:./Vhmc/App/AutomaticInfraredAudioLeveler
Set-Location ..
Get-Date -Format G
Write-Output 'Deployed to Raspberry!'
