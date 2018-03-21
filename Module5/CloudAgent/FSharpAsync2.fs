namespace FSharpAsync2

open Microsoft.WindowsAzure
open System.IO
open Microsoft.WindowsAzure.Storage
open Microsoft.WindowsAzure.Storage.Blob

// Asynchronous image downloader
// Using async {...}, code structure changes very little from synchronous
// No concurrency (yet) - all code runs on the UI thread that "DownloadAll" is called on
// UI doesn't block, handles UI events while waiting for I/O

type AzureImageDownloader(folder) =
    let acct = CloudStorageAccount.Parse (Settings.connection)

    let storage = acct.CreateCloudBlobClient();
    let container = storage.GetContainerReference(folder);
    let _ = container.CreateIfNotExists()

    let downloadImageAsync(blob : CloudBlockBlob) =
      async {
        let data = Array.zeroCreate<byte> (int blob.Properties.Length)
        let! pixels = blob.DownloadToByteArrayAsync(data, 0) |> Async.AwaitTask
        let fileName = "thumbs-" + blob.Uri.Segments.[blob.Uri.Segments.Length-1]
        use outStream =  File.OpenWrite(fileName)
        do! outStream.AsyncWrite(data, 0, pixels)
        return fileName
      }

    member this.DownloadAll() =
      async {
        for blob in container.ListBlobs() do
            let! name = downloadImageAsync(container.GetBlockBlobReference(blob.Uri.ToString()) )
            ()
      }
      |> Async.StartImmediate