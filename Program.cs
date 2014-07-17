namespace LargeFileUploader
{
    using System;

    class Program
    {

        static void Main(string[] args)
        {
            LargeFileUploaderUtils.Log = Console.Out.WriteLine;
            // LargeFileUploaderUtils.NumBytesPerChunk = 1 * 1024;
            
            LargeFileUploaderUtils.UploadAsync(
                inputFile: @"C:\Users\chgeuer\github\chgeuer\AzureLargeFileUploader\bin\Debug\LargeFileUploader.exe",
                storageConnectionString: Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING"),
                containerName: "dummy222222",
                uploadParallelism: 2).Wait();        
        }
    }
}