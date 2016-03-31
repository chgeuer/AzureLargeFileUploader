namespace LargeFileUploader
{
    using System;
    using System.IO;
    using System.Text;

    class Program
    {

        static void Main(string[] args)
        {
            LargeFileUploaderUtils.Log = Console.Out.WriteLine;
            LargeFileUploaderUtils.NumBytesPerChunk = 1 * 1024 * 1024;

            var connectionString = Environment.GetEnvironmentVariable("AzureStorageAccount1");

            //LargeFileUploaderUtils.UploadAsync(
            //    inputFile: @"C:\Users\chgeuer\format504015.mp4",
            //    storageConnectionString: connectionString,
            //    containerName: "dummy222222",
            //    uploadParallelism: 2).Wait();

            //var address = new FileInfo(@"C:\Users\chgeuer\ard.mp4").UploadAsync(
            //    storageAccount: connectionString.ToStorageAccount(),
            //    containerName: "foo",
            //    blobName: "bar/baz/1.mp4",
            //    uploadParallelism: 4).Result;

            connectionString
                .ToStorageAccount()
                .CreateCloudBlobClient()
                .GetContainerReference("target")
                .GetBlockBlobReference("Birthday card.mp4")
                .DownloadRecomputeAndSetMD5Async()
                .Wait();

            //var address = LargeFileUploaderUtils.UploadAsync(
            //    inputFile:  ,
            //    storageConnectionString: "DefaultEndpointsProtocol=https;AccountName=xxxxx;AccountKey=......................==",
            //    containerName: "ard", 
            //    uploadParallelism: 5).Result;
        }
    }
}