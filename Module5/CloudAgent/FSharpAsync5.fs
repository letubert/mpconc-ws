namespace FSharpAsync5

open Microsoft.WindowsAzure
open Microsoft.WindowsAzure.Storage
open System.IO
open Microsoft.WindowsAzure.Storage.Blob

// Parallel agent-based image downloader
// Agents (MailboxProcessor) provide building-block for other primitives - like parallelWorker
// Re-introduces parallelism

type AzureImageDownloader(folder) =
    let acct = CloudStorageAccount.Parse (Settings.connection)
    let storage = acct.CreateCloudBlobClient();
    let container = storage.GetContainerReference(folder);


    let downloadImageAsync(blob : CloudBlockBlob) =
      async {
        let! _ = container.CreateIfNotExistsAsync() |> Async.AwaitTask
        let data = Array.zeroCreate<byte> ((int)blob.Properties.Length)
        let! pixels = blob.DownloadToByteArrayAsync(data, 0) |> Async.AwaitTask
        let fileName = "thumbs-" + blob.Uri.Segments.[blob.Uri.Segments.Length-1]
        use outStream =  File.OpenWrite(fileName)
        do! outStream.AsyncWrite(data, 0, pixels)
        return fileName
      }

    let parallelWorker n f =
        MailboxProcessor.Start(fun inbox ->
            let workers = Array.init n (fun i -> MailboxProcessor.Start(f))
            let rec loop i = async {

                let! msg = inbox.Receive()
                workers.[i].Post(msg)
                return! loop ((i+1) % n)

            }
            loop 0
        )






    // TODO : 5.18
    // A reusable parallel worker model built on F# agents
    // implement a parallel worker based on MailboxProcessor, which coordinates the work in a Round-Robin fashion
    // between a set of children MailboxProcessor(s)
    // use an Array initializer to create the collection of MailboxProcessor(s)
    // the internal state should keep track of the index of the child to the send  the next message

    let parallelWorker_TODO n (f:MailboxProcessor<_> -> Async<unit>) = ()
        // MailboxProcessor.Start(fun inbox ->

    let agent =
        parallelWorker 8 (fun inbox ->
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


