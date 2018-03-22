namespace FSharpAsync1

open Microsoft.WindowsAzure
open System.IO
open Microsoft.WindowsAzure.Storage
open Microsoft.WindowsAzure.Storage.Blob

// Synchronous image donwloader
// Code is simple and intuitive
// ... but UI freezes

type AzureImageDownloader(folder) =
    let acct = CloudStorageAccount.Parse(Settings.connection)

    let storage = acct.CreateCloudBlobClient();
    let container = storage.GetContainerReference(folder);
    let _ = container.CreateIfNotExists()

    let downloadImage(blob : CloudBlockBlob) =
        let data = Array.zeroCreate<byte> (int blob.Properties.Length)
        let pixels = blob.DownloadToByteArray(data, 0)
        let fileName = "thumbs-" + blob.Uri.Segments.[blob.Uri.Segments.Length-1]
        use outStream =  File.OpenWrite(fileName)
        do outStream.Write(data, 0, pixels)
        fileName

    member this.DownloadAll() =
        for blob in container.ListBlobs() do
            let name = downloadImage(container.GetBlockBlobReference(blob.Uri.ToString()) )
            ()
