using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Configuration;
using Microsoft.WindowsAzure.Storage.Blob;

namespace LargeFileUploader
{
    class Program
    {
        const int kB = 1024;
        const int MB = kB * 1024;
        const long GB = MB * 1024;
        const int NumBytesPerChunk = 512 * kB; //1 MB per block

        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("Bitte die Video-Datei zum Hochladen mit angeben...");
                return;
            }

            var filename = args[0];
            if (!File.Exists(filename))
            {
                Console.Error.WriteLine("Video-Datei \"{0}\" existiert nicht?", filename);
                return;
            }

            Console.WriteLine("Uploading {0}", filename);

            var connectionString = ConfigurationManager.AppSettings["storageaccount"];
            Console.WriteLine("Using connection " + connectionString);
            var storageAccount = CloudStorageAccount.Parse(connectionString);

            upload(new FileInfo(filename), storageAccount);
        }

        public static byte[] GetFileContent(FileInfo file, long offset, int length)
        {
            using (var stream = file.OpenRead())
            {
                stream.Seek(offset, SeekOrigin.Begin);

                byte[] contents = new byte[length];
                var len = stream.Read(contents, 0, contents.Length);
                if (len == length)
                {
                    return contents;
                }

                byte[] rest = new byte[len];
                Array.Copy(contents, rest, len);
                return rest;
            }
        }

        private static void consoleExceptionHandler(Exception ex)
        {
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine("Problem occured, trying again. Details of the problem: ");
            for (var e = ex; e != null; e = e.InnerException)
            {
                Console.Error.WriteLine(e.Message);
            }
            Console.Error.WriteLine("---------------------------------------------------------------------");
            Console.Error.WriteLine(ex.StackTrace);
            Console.ResetColor();
            Console.Error.WriteLine("---------------------------------------------------------------------");
        }

        public static void upload(FileInfo file, CloudStorageAccount storageAccount)
        {
            var blobName = file.Name;

            Action<Action, Action<Exception>> executeUntilSuccess = (action, exceptionHandler) =>
            {
                bool success = false;
                while (!success)
                {
                    try
                    {
                        action();
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        if (exceptionHandler != null) { exceptionHandler(ex); }
                    }
                }
            };

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("mycontainer");
            container.CreateIfNotExists();

            var permission = container.GetPermissions();
            permission.PublicAccess = BlobContainerPublicAccessType.Container;
            container.SetPermissions(permission);

            CloudBlockBlob blob = container.GetBlockBlobReference(blobName);

            List<string> blocklist = new List<string>();

            Action saveListToFile = () =>
            {
                var text = new StringBuilder();
                text.AppendLine("File:         " + file.FullName);
                text.AppendLine("Length:       " + file.Length);
                text.AppendLine("Chunk Size:   " + NumBytesPerChunk);
                text.AppendLine("Blobname:     " + blobName);
                text.AppendLine("BlobEndpoint: " + storageAccount.BlobEndpoint.AbsoluteUri);
                blocklist.ForEach(_ => text.AppendLine(_));

                File.WriteAllText("log.txt", text.ToString());
            };

            Console.WriteLine("Starting upload in chunks of {0} bytes", NumBytesPerChunk);

            DateTime initialStartTime = DateTime.UtcNow;
            int id = 0;
            for (long index = 0; index < file.Length; index += NumBytesPerChunk, id++)
            {
                byte[] buffer = GetFileContent(file, index, NumBytesPerChunk);
                string blockIdBase64 = Convert.ToBase64String(System.BitConverter.GetBytes(id));

                DateTime start = DateTime.UtcNow;

                executeUntilSuccess(() => blob.PutBlock(blockIdBase64, new MemoryStream(buffer, true), null), consoleExceptionHandler);
                executeUntilSuccess(() => saveListToFile(), consoleExceptionHandler);
                blocklist.Add(blockIdBase64);

                var kbPerSec = (((double)buffer.Length) / (DateTime.UtcNow.Subtract(start).TotalSeconds * 1024));
                var MBPerMin = (((double)buffer.Length) / (DateTime.UtcNow.Subtract(start).TotalMinutes * 1024 * 1024));

                Func<long, long, string> absoluteProgress = (current, total) =>
                {
                    if (file.Length < kB)
                    {
                        // Bytes is reasonable
                        return string.Format("{0} of {1} bytes", current, total);
                    }
                    else if (file.Length < 10 * MB)
                    {
                        // kB is a reasonable unit
                        return string.Format("{0} of {1} kByte", (current / kB), (total / kB));
                    }
                    else if (file.Length < 10 * GB)
                    {
                        // MB is a reasonable unit
                        return string.Format("{0} of {1} MB", (current / MB), (total / MB));
                    }
                    else
                    {
                        // GB is a reasonable unit
                        return string.Format("{0} of {1} GB", (current / GB), (total / GB));
                    }
                };

                Func<long, long, string> relativeProgress = (current, total) => string.Format(
                    "{0} %", (100.0 * current / total).ToString("F3"));

                Func<long, long, string> estimatedArrivalTime = (current, total) =>
                {
                    double elapsedSeconds = DateTime.UtcNow.Subtract(initialStartTime).TotalSeconds;
                    double progress = ((double)current) / ((double)total);

                    if (current == 0) return "unknown time";

                    double remainingSeconds = elapsedSeconds * (1 - progress) / progress;

                    TimeSpan remaining = TimeSpan.FromSeconds(remainingSeconds);

                    return string.Format("{0} remaining, (expect to finish by {1} local time)",
                        remaining.ToString("g"), 
                        DateTime.Now.ToLocalTime().Add(remaining));
                };

                Console.WriteLine(
                    "Uploaded {0} ({1}) with {2} kB/sec ({3} MB/min), {4}",
                    absoluteProgress(index + buffer.Length, file.Length),
                    relativeProgress(index + buffer.Length, file.Length),
                    kbPerSec.ToString("F0"),
                    MBPerMin.ToString("F1"),
                    estimatedArrivalTime(index + buffer.Length, file.Length));
            }

            executeUntilSuccess(() => blob.PutBlockList(blocklist), consoleExceptionHandler);
            executeUntilSuccess(() => saveListToFile(), consoleExceptionHandler);
            Console.WriteLine("PutBlockList succeeded, finished upload to {0}", blob.Uri.AbsoluteUri);
        }
    }
}