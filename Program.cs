namespace LargeFileUploader
{
    using System;

    class Program
    {

        static void Main(string[] args)
        {
            LargeFileUploaderUtils.Log = Console.Out.WriteLine;
            LargeFileUploaderUtils.NumBytesPerChunk = 512 * 1024;

            LargeFileUploaderUtils.UploadAsync(
                inputFile: @"C:\Users\chgeuer\format504015.mp4", 
                storageConnectionString: "DefaultEndpointsProtocol=https;AccountName=accountname;AccountKey=XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX==",
                containerName: "dummy1",
                uploadParallelism: 2).Wait();        
        }
    }
}