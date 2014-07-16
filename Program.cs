namespace LargeFileUploader
{
    using System.IO;
    using System.Threading.Tasks;
    using global::Microsoft.WindowsAzure.Storage;
    using global::Microsoft.WindowsAzure.Storage.Auth;

    class Program
    {

        static void Main(string[] args)
        {
            new FileInfo(@"C:\Users\chgeuer\Desktop\1.mp4")
                .UploadAsync(
                    storageAccount: CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=storageaccount123;AccountKey=XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX=="), 
                    containerName: "myfolder")
                .Wait();
        }
    }
}