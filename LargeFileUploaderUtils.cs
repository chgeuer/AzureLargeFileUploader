namespace LargeFileUploader
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;

    using global::Microsoft.WindowsAzure.Storage;
    using global::Microsoft.WindowsAzure.Storage.Blob;

    public static class LargeFileUploaderUtils
    {
        const int kB = 1024;
        const int MB = kB * 1024;
        const long GB = MB * 1024;
        public static int NumBytesPerChunk = 4 * MB; // A block may be up to 4 MB in size. 


        static LargeFileUploaderUtils()
        {
            LargeFileUploaderUtils.Log = _ => Console.WriteLine(_);
        }

        public static Action<string> Log { get; set; }

        private static void log(string format, params object[] args)
        {
            if (Log != null) { Log(string.Format(format, args)); }
        }

        public static async Task<byte[]> GetFileContentAsync(this FileInfo file, long offset, int length)
        {
            using (var stream = file.OpenRead())
            {
                stream.Seek(offset, SeekOrigin.Begin);

                byte[] contents = new byte[length];
                var len = await stream.ReadAsync(contents, 0, contents.Length);
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
            log("Problem occured, trying again. Details of the problem: ");
            for (var e = ex; e != null; e = e.InnerException)
            {
                Console.Error.WriteLine(e.Message);
            }
            log("---------------------------------------------------------------------");
            log(ex.StackTrace);
            log("---------------------------------------------------------------------");
        }

        public static async Task UploadAsync(string inputFile, string storageConnectionString, string containerName)
        {
            await new FileInfo(inputFile).UploadAsync(CloudStorageAccount.Parse(storageConnectionString), containerName);
        }

        public static async Task ExecuteUntilSuccessAsync(Func<Task> action, Action<Exception> exceptionHandler)
        {
            bool success = false;
            while (!success)
            {
                try
                {
                    await action();
                    success = true;
                }
                catch (Exception ex)
                {
                    if (exceptionHandler != null) { exceptionHandler(ex); }
                }
            }
        }

        internal class BlockMetadata
        {
            public BlockMetadata(int id, long length, int bytesPerChunk)
            {
                this.Id = id;
                this.BlockId = Convert.ToBase64String(System.BitConverter.GetBytes(id));
                this.Index = ((long) id) * ((long)bytesPerChunk);
                long remainingBytesInFile = length - this.Index;
                this.Length = (int) Math.Min(remainingBytesInFile, (long)bytesPerChunk);
            }

            public long Index { get; private set; }
            public int Id { get; private set; }
            public string BlockId { get; private set; }
            public int Length { get; private set; }
        }

        public static Task ForEachAsync<T>(this IEnumerable<T> source, int parallelUploads, Func<T, Task> body)
        {
            return Task.WhenAll(
                Partitioner
                .Create(source)
                .GetPartitions(parallelUploads)
                .Select(partition => Task.Run(async () =>
                        {
                            using (partition)
                            {
                                while (partition.MoveNext())
                                {
                                    await body(partition.Current);
                                }
                            }
                        })));
        }

        public static async Task UploadAsync(this FileInfo file, CloudStorageAccount storageAccount, string containerName)
        {
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(containerName);
            await container.CreateIfNotExistsAsync();

            if (NumBytesPerChunk > 4 * MB) NumBytesPerChunk = 4 * MB;
            var blobName = file.Name;
            long fileLength = file.Length;
            var blockBlob = container.GetBlockBlobReference(blobName);

            var allBlockInFile = Enumerable
                 .Range(0, 1 + ((int)(fileLength / NumBytesPerChunk)))
                 .Select(_ => new BlockMetadata(_, fileLength, NumBytesPerChunk))
                 .ToList();
            var blockIdList = allBlockInFile.Select(_ => _.BlockId).ToList();

            List<BlockMetadata> blocksToUpload = null;

            try
            {
                var existingBlocks = (await blockBlob.DownloadBlockListAsync(
                        BlockListingFilter.Uncommitted,
                        AccessCondition.GenerateEmptyCondition(),
                        new BlobRequestOptions { },
                        new OperationContext()))
                    .Where(_ => _.Length == NumBytesPerChunk)
                    .ToList();

                blocksToUpload = allBlockInFile.Where(blockInFile => !existingBlocks.Any(existingBlock => existingBlock.Name == blockInFile.BlockId)).ToList();
            }
            catch (StorageException) 
            {
                blocksToUpload = allBlockInFile;
            }

            var md5 = ((Func<Func<byte[], string>>)(() =>
            {
                var hashFunction = MD5.Create();
                return (content) => Convert.ToBase64String(hashFunction.ComputeHash(content));
            }));

            DateTime initialStartTime = DateTime.UtcNow;
            await LargeFileUploaderUtils.ForEachAsync(
                source: allBlockInFile,
                parallelUploads: 4, 
                body: async (block) =>
                {
                    byte[] blockData = await GetFileContentAsync(file, block.Index, block.Length);
                    string contentHash = md5()(blockData);

                    DateTime start = DateTime.UtcNow;
                    await ExecuteUntilSuccessAsync(async () =>
                    {
                        await blockBlob.PutBlockAsync(block.BlockId, new MemoryStream(blockData, true), contentHash);
                    }, consoleExceptionHandler);

                    #region Statistics

                    var kbPerSec = (((double)block.Length) / (DateTime.UtcNow.Subtract(start).TotalSeconds * kB));
                    var MBPerMin = (((double)block.Length) / (DateTime.UtcNow.Subtract(start).TotalMinutes * MB));

                    Func<long, long, string> absoluteProgress = (current, total) =>
                    {
                        if (fileLength < kB)
                        {
                            // Bytes is reasonable
                            return string.Format("{0} of {1} bytes", current, total);
                        }
                        else if (fileLength < 10 * MB)
                        {
                            // kB is a reasonable unit
                            return string.Format("{0} of {1} kByte", (current / kB), (total / kB));
                        }
                        else if (fileLength < 10 * GB)
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

                    log(
                        "Uploaded {0} ({1}) with {2} kB/sec ({3} MB/min), {4}",
                        absoluteProgress(block.Index + block.Length, fileLength),
                        relativeProgress(block.Index + block.Length, fileLength),
                        kbPerSec.ToString("F0"),
                        MBPerMin.ToString("F1"),
                        estimatedArrivalTime(block.Index + block.Length, fileLength));

                    #endregion
                });

            await ExecuteUntilSuccessAsync(async () => 
            { 
                await blockBlob.PutBlockListAsync(blockIdList); 
            }, consoleExceptionHandler);

            log("PutBlockList succeeded, finished upload to {0}", blockBlob.Uri.AbsoluteUri);
        }
    }
}
