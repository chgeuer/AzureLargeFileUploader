namespace LargeFileUploader
{
    using Microsoft.WindowsAzure.Storage;
    using System;
    using System.IO;
    using System.Text;

    class Program
    {

        static void Main(string[] args)
        {
            LargeFileUploaderUtils.Log = Console.Out.WriteLine;
            LargeFileUploaderUtils.NumBytesPerChunk = 1 * 1024 * 1024;

            //LargeFileUploaderUtils.UploadAsync(
            //    inputFile: @"C:\Users\chgeuer\format504015.mp4",
            //    storageConnectionString: Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING"),
            //    containerName: "dummy222222",
            //    uploadParallelism: 2).Wait();

            var address = new FileInfo(@"C:\Users\chgeuer\ard.mp4").UploadAsync(
                storageAccount: "DefaultEndpointsProtocol=https;AccountName=xxxxx;AccountKey=......................==".ToStorageAccount(),
                containerName: "foo",
                blobName: "bar/baz/1.mp4",
                uploadParallelism: 4).Result;

            //var address = LargeFileUploaderUtils.UploadAsync(
            //    inputFile:  ,
            //    storageConnectionString: "DefaultEndpointsProtocol=https;AccountName=xxxxx;AccountKey=......................==",
            //    containerName: "ard", 
            //    uploadParallelism: 5).Result;
        }
    }
}