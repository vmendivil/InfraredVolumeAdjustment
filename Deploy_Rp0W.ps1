$SshPrivateKey = "C:\Vhmc\Msi2Pi0W\Msi2Pi0W"
$PublishPath = ".\bin\Debug\netcoreapp3.1\*"

Set-Location Raspberry.VolumeLeveler
#Remove-Item $PublishPath -r
dotnet build -c Debug
#dotnet publish --force -c Debug
#ssh pi@192.168.1.61 -i $SshPrivateKey "sudo rm -r ./Vhmc/Apps/InfraredAudioLeveler/*"
scp -i $SshPrivateKey -r $PublishPath pi@192.168.1.61:./Vhmc/Apps/InfraredAudioLeveler
Set-Location ..
Get-Date -Format G
Write-Output 'Deployed to Raspberry!'
