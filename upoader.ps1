
$rootfolder = "C:\Users\chgeuer\github\chgeuer\AzureLargeFileUploader"
$jsonAssembly = [System.Reflection.Assembly]::LoadFrom("$rootfolder\packages\Newtonsoft.Json.6.0.3\lib\net45\Newtonsoft.Json.dll")
$storageAssembly = [System.Reflection.Assembly]::LoadFrom("$rootfolder\packages\WindowsAzure.Storage.4.1.0\lib\net40\Microsoft.WindowsAzure.Storage.dll")
$cscode = ((New-Object -TypeName System.Net.WebClient).DownloadString("https://github.com/chgeuer/AzureLargeFileUploader/raw/master/LargeFileUploaderUtils.cs"))
Add-Type -TypeDefinition $cscode -ReferencedAssemblies $jsonAssembly.Location,$storageAssembly.Location


[LargeFileUploader.LargeFileUploaderUtils]::UseConsoleForLogging()
[LargeFileUploader.LargeFileUploaderUtils]::NumBytesPerChunk = 1024

$storageaccount = [System.Environment]::GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")
$containername = "dummyps1"

Write-Host "Start upload"
$task = [LargeFileUploader.LargeFileUploaderUtils]::UploadAsync("C:\Users\chgeuer\github\chgeuer\AzureLargeFileUploader\LargeFileUploader.csproj", $storageaccount, $containername, 2)
Write-Host "Upload started"
$task.Wait()
Write-Host "Upload finished"

