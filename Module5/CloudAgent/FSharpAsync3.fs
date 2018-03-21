namespace FSharpAsync3

open Microsoft.WindowsAzure
open System.IO
open Microsoft.WindowsAzure.Storage
open Microsoft.WindowsAzure.Storage.Blob

// Parallel and async image downloader
// Maximally leverages CPU and I/O parallelism
// Potentially has multiple pending network requests, multiple pending disk writes and multiple cores busy concurrently
// Async objects support functional compositional programming model

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
      container.ListBlobs()
      |> Seq.map (fun blob ->
        downloadImageAsync(container.GetBlockBlobReference(blob.Uri.ToString())))
      |> Async.Parallel
      |> Async.StartAsTask