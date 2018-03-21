using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using CloudHelpers;

namespace AsyncBlobCloud.Asynchronous
{
    public class AsyncBlobCloud
    {
        private readonly int bufferSize = 0x1000;

        public void DownloadMedia(string folderPath)
        {
            var container = Helpers.GetCloudBlobContainer();

            foreach (var blob in container.ListBlobs())
            {
                var blobName = blob.Uri.Segments[blob.Uri.Segments.Length - 1];
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);

                using (var fileStream = new FileStream(Path.Combine(folderPath, blobName),
                                            FileMode.Create, FileAccess.Write, FileShare.None, bufferSize))
                {
                    blockBlob.DownloadToStream(fileStream);
                }
            }
        }

        public async Task DownloadMediaAsync(string folderPath,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var container = await Helpers.GetCloudBlobContainerAsync(cancellationToken);

            foreach (var blob in container.ListBlobs())
            {
                // Retrieve reference to a blob
                var blobName = blob.Uri.Segments[blob.Uri.Segments.Length - 1];
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);

                using (var fileStream = new FileStream(Path.Combine(folderPath, blobName),
                                        FileMode.Create, FileAccess.Write, FileShare.Read, bufferSize, FileOptions.Asynchronous))
                {
                    await blockBlob.DownloadToStreamAsync(fileStream, cancellationToken).ConfigureAwait(false);
                    fileStream.Close();
                }
            }
        }

        // TODO : 4.3
        // Run the download operations in parallel
        public async Task DownloadInParallelAsync(string folderPath,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            //var container = Helpers.GetCloudBlobContainer();
            var container = await Helpers.GetCloudBlobContainerAsync(cancellationToken);
            var blobs = container.ListBlobs();

            // Create a query that, when executed, returns a collection of tasks.
            IEnumerable<Task> tasks =
                blobs.Select(blob =>
                        DownloadMedia(blob.Uri.Segments[blob.Uri.Segments.Length - 1], folderPath, cancellationToken));

            // Use ToList to execute the query and start the tasks.
            Task[] downloadTasks = null;

            // wait all to complete
        }

        // TODO : 4.4
        public Task DownloadInParallelExecuteComplete(string folderPath,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.Run(async () =>
            {
                //var container = Helpers.GetCloudBlobContainer();
                var container = await Helpers.GetCloudBlobContainerAsync(cancellationToken);
                var blobs = container.ListBlobs();

                // ***Create a query that, when executed, returns a collection of tasks.
                IEnumerable<Task> tasks =
                    blobs.Select(blob =>
                            DownloadMedia(blob.Uri.Segments[blob.Uri.Segments.Length - 1], folderPath, cancellationToken));

                // TODO
                // execute the task in parallel
                // ***Use ToList to execute the query and start the tasks.
                List<Task> downloadTasks = null;

                // ***Add a loop to process the tasks one at a time until none remain.
                while (downloadTasks.Count > 0)
                {
                    // code here
                }
            });
        }

        private async Task DownloadMedia(string blobReference, string folderPath,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var container = await Helpers.GetCloudBlobContainerAsync(cancellationToken).ConfigureAwait(false);
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobReference);
            using (var memStream = new MemoryStream())
            {
                await blockBlob.DownloadToStreamAsync(memStream).ConfigureAwait(false);
                await memStream.FlushAsync().ConfigureAwait(false);
                byte[] data = memStream.ToArray();
                using (var fileStream = new FileStream(Path.Combine(folderPath, blobReference),
                                        FileMode.Create, FileAccess.Write, FileShare.ReadWrite, bufferSize, FileOptions.Asynchronous))
                {
                    await fileStream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
                }
            }
        }
    }
}