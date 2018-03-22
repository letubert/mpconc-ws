namespace FSharpAsync4

open Microsoft.WindowsAzure
open System.IO
open Microsoft.WindowsAzure.Storage
open Microsoft.WindowsAzure.Storage.Blob

// Agent-based image downloader
// Very simple agent accepts string messages indicating blobs to download
// Agent encapsulates the concurrent behaviour of the system, replaces ad-hoc concurrency in previous examples
// No parallelism, agent handles requests serially, but asynchronously

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

    let agent =
        MailboxProcessor.Start(fun inbox ->
            let rec loop() = async {
                let! msg = inbox.Receive()
                let blob = container.GetBlockBlobReference(msg)
                let! fileName = downloadImageAsync(blob)
                return! loop()

            }
            loop()
        )

    member this.DownloadAll() =
      for blob in container.ListBlobs() do
        agent.Post(blob.Uri.ToString())

