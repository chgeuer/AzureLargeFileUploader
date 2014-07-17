
$jsonAssembly = [System.Reflection.Assembly]::LoadFrom("C:\Users\chgeuer\github\chgeuer\AzureLargeFileUploader\packages\Newtonsoft.Json.6.0.1\lib\net45\Newtonsoft.Json.dll")
$storageAssembly = [System.Reflection.Assembly]::LoadFrom("C:\Users\chgeuer\github\chgeuer\AzureLargeFileUploader\packages\WindowsAzure.Storage.3.0.2.0\lib\net40\Microsoft.WindowsAzure.Storage.dll")

$cscode = ((New-Object -TypeName System.Net.WebClient).DownloadString("https://github.com/chgeuer/AzureLargeFileUploader/raw/master/LargeFileUploaderUtils.cs"))

Add-Type -TypeDefinition $cscode -ReferencedAssemblies $jsonAssembly.Location,$storageAssembly.Location

[LargeFileUploader.LargeFileUploaderUtils]::Log = [System.Console]::WriteLine
[LargeFileUploader.LargeFileUploaderUtils]::UploadAsync("C:\Users\chgeuer\format504015.mp4", "DefaultEndpointsProtocol=https;AccountName=chgeuerams2;AccountKey=XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX==", "dummy1", 2).Wait()
Write-Host "Done"
