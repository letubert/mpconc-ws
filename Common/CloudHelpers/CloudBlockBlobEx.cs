﻿using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CloudHelpers
{
    public static class CloudBlockBlobEx
    {
        public static async Task DownloadToFileAsync(CloudBlobContainer container, int bufferSize, string blobName, string fileDestination, CancellationTokenSource cts)
        {
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);
            using (var blobStream = await blockBlob.OpenReadAsync(cts.Token))
            using (var fileStream = new FileStream(fileDestination, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, bufferSize, FileOptions.Asynchronous))
            {
                byte[] buffer = new byte[blockBlob.Properties.Length];
                int bytesRead = await blobStream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                await fileStream.WriteAsync(buffer, 0, bytesRead, cts.Token);
            }
        }
    }

    public static class Helpers
    {
        // TODO
        private static readonly string Connection = "DefaultEndpointsProtocol=https;AccountName=rikdata;AccountKey=SFVkivJbEPcEUR2PR6iXTP44uBUfL5suo7JPDL8XX2e+IBzOzL2/jjC5Rxerro8zXIqf+AspQHsBTygfz/HfbA==;EndpointSuffix=core.windows.net";

        public static async Task<CloudBlobContainer> GetCloudBlobContainerAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Helpers.Connection);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("myblobpics");

            // Create the container if it doesn't already exist.
            await container.CreateIfNotExistsAsync(cancellationToken);
            return container;
        }

        public static CloudBlobContainer GetCloudBlobContainer(CancellationToken cancellationToken = default(CancellationToken))
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Helpers.Connection);
            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            // Retrieve a reference to a container.
            CloudBlobContainer container = blobClient.GetContainerReference("myblobpics");
            // Create the container if it doesn't already exist.
            container.CreateIfNotExists();
            return container;
        }
    }
}
