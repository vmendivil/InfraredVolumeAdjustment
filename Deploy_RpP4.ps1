$SshPrivateKey = "C:\Vhmc\Msi2Pi4\Msi2Pi4"
$PublishPath = ".\bin\Debug\netcoreapp3.1\*"

# Console application
Set-Location Raspberry.VolumeLeveler
#Remove-Item $PublishPath -r
dotnet build -c Debug
#dotnet publish --force -c Debug
#ssh pi@192.168.1.48 -i $SshPrivateKey "sudo rm -r ./Vhmc/App/IRAudioLeveler/Console/*"
scp -i $SshPrivateKey -r $PublishPath pi@192.168.1.48:./Vhmc/App/IRAudioLeveler/Console
Set-Location ..

# Api application
Set-Location Raspberry.Api
#Remove-Item $PublishPath -r
dotnet build -c Debug
#dotnet publish --force -c Debug
#ssh pi@192.168.1.48 -i $SshPrivateKey "sudo rm -r ./Vhmc/App/IRAudioLeveler/Api/*"
scp -i $SshPrivateKey -r $PublishPath pi@192.168.1.48:./Vhmc/App/IRAudioLeveler/Api
Set-Location ..

Get-Date -Format G
Write-Output 'Deployed to Raspberry!'
