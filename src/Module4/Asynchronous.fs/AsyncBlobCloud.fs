namespace AsyncBlobCloudFS

open Microsoft.WindowsAzure.Storage
open System.IO
open Microsoft.WindowsAzure.Storage.Blob
open System
open System.Threading
open System.Threading.Tasks
open System.Net
open System.Drawing
open FunctionalConcurrency

// TODO use Request Gate throttle


[<AutoOpen>]
module Helpers =
    let azureConnection = "< Azure Connection >"

    let bufferSize = 0x1000
    let cts = new CancellationTokenSource()

    let getListBlobMedia (container:CloudBlobContainer) =
        let blobs = container.ListBlobs()
        blobs
        |> Seq.map(fun blob ->
            blob.Uri.Segments.[blob.Uri.Segments.Length - 1])


module CodeSnippets =
    open ImageProcessing
    open FunctionalConcurrency.AsyncOperators

    // Asynchronous-Workflow implementation of image-download
    let getCloudBlobContainerAsync() : Async<CloudBlobContainer> = async {
        let storageAccount = CloudStorageAccount.Parse(azureConnection)
        let blobClient = storageAccount.CreateCloudBlobClient()
        let container = blobClient.GetContainerReference("media")
        let! _ = container.CreateIfNotExistsAsync()
        return container }

    let downloadMediaAsync(fileNameDestination:string) (blobNameSource:string) =
      async {
        let! container = getCloudBlobContainerAsync()
        let blockBlob = container.GetBlockBlobReference(blobNameSource)
        let! (blobStream : Stream) = blockBlob.OpenReadAsync()

        use fileStream = new FileStream(fileNameDestination, FileMode.Create, FileAccess.Write, FileShare.None, 0x1000, FileOptions.Asynchronous)
        let buffer = Array.zeroCreate<byte> (int blockBlob.Properties.Length)
        let rec copyStream bytesRead = async {
            match bytesRead with
            | 0 -> fileStream.Close()
                   blobStream.Close()
            | n -> do! fileStream.AsyncWrite(buffer, 0, n)
                   let! bytesRead = blobStream.AsyncRead(buffer, 0, buffer.Length)
                   return! copyStream bytesRead }

        let! bytesRead = blobStream.AsyncRead(buffer, 0, buffer.Length)
        do! copyStream bytesRead  }


    // De-Sugared DownloadMediaAsync computation expression
    let downloadMediaAsyncDeSugar(blobNameSource:string) (fileNameDestination:string) =
        async.Delay(fun() ->
            async.Bind(getCloudBlobContainerAsync(), fun container ->
                let blockBlob = container.GetBlockBlobReference(blobNameSource)
                async.Bind(blockBlob.OpenReadAsync(), fun (blobStream:Stream) ->
                    let sizeBlob = int blockBlob.Properties.Length
                    async.Bind(blobStream.AsyncRead(sizeBlob), fun bytes ->
                        use fileStream = new FileStream(fileNameDestination, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, FileOptions.Asynchronous)
                        async.Bind(fileStream.AsyncWrite(bytes, 0, bytes.Length), fun () ->
                            fileStream.Close()
                            blobStream.Close()
                            async.Return())))))


    // AsyncRetry Computation Expression
    type RetryAsyncBuilder(max, sleepMilliseconds : int) =
        let rec retry n (task:Async<'a>) (continuation:'a -> Async<'b>) = async {
            try
                let! result = task
                let! conResult = continuation result
                return conResult
            with error ->
                if n = 0 then return raise error
                else
                    do! Async.Sleep sleepMilliseconds
                    return! retry (n - 1) task continuation }

        member x.ReturnFrom(f) = f
        member x.Return(v) = async { return v }
        member x.Delay(f) = async { return! f() }
        member x.Bind(task:Async<'a>, continuation:'a -> Async<'b>) =
                                        retry max task continuation
        member x.Bind(task : Task<'T>, continuation : 'T -> Async<'R>) : Async<'R> = x.Bind(Async.AwaitTask task, continuation)


    // Retry Async Builder
    let retry = RetryAsyncBuilder(3, 250)

    let downloadMediaCompRetryAsync (blobNameSource:string) (fileNameDestination:string) =
      async {
        let! container = retry {
            return! getCloudBlobContainerAsync() }
        let blockBlob = container.GetBlockBlobReference(blobNameSource)
        let! (blobStream : Stream) = blockBlob.OpenReadAsync()

        use fileStream = new FileStream(fileNameDestination, FileMode.Create, FileAccess.Write, FileShare.None, 0x1000, FileOptions.Asynchronous)
        let buffer = Array.zeroCreate<byte> (int blockBlob.Properties.Length)
        let rec copyStream bytesRead = async {
            match bytesRead with
            | 0 -> fileStream.Close(); blobStream.Close()
            | n -> do! fileStream.AsyncWrite(buffer, 0, n)
                   let! bytesRead = blobStream.AsyncRead(buffer, 0, buffer.Length)
                   return! copyStream bytesRead }

        let! bytesRead = blobStream.AsyncRead(buffer, 0, buffer.Length)
        do! copyStream bytesRead  }


    // Extending the asynchronous workflow to support Task<'a>
    type Microsoft.FSharp.Control.AsyncBuilder with
        member x.Bind(t : Task<'T>, f : 'T -> Async<'R>) : Async<'R> = async.Bind(Async.AwaitTask t, f)
        member x.ReturnFrom(computation : Task<'T>) = x.ReturnFrom(Async.AwaitTask computation)
        member x.Bind(t : Task, f : unit -> Async<'R>) : Async<'R> = async.Bind(Async.AwaitTask t, f)
        member x.ReturnFrom(computation : Task) = x.ReturnFrom(Async.AwaitTask computation)


    // Async.Parallel downloads all images in parallel
    let downloadMediaCompAsync (container:CloudBlobContainer)
                               (blobMedia:IListBlobItem) = retry {
        let blobName = blobMedia.Uri.Segments.[blobMedia.Uri.Segments.Length-1]
        let blockBlob = container.GetBlockBlobReference(blobName)
        let! (blobStream : Stream) = blockBlob.OpenReadAsync()
        return Bitmap.FromStream(blobStream)
    }

    let transormAndSaveImage (container:CloudBlobContainer)
                             (blobMedia:IListBlobItem) =
        downloadMediaCompAsync container blobMedia
        |> AsyncEx.map ImageHelpers.setGrayscale
        |> AsyncEx.map ImageHelpers.createThumbnail
        |> AsyncEx.tee (fun image ->
                let mediaName =
                    blobMedia.Uri.Segments.[blobMedia.Uri.Segments.Length - 1]
                image.Save(mediaName))

    let downloadMediaCompAsyncParallel() = retry {
        let! container = getCloudBlobContainerAsync()
        let computations =
            container.ListBlobs()
            |> Seq.map(transormAndSaveImage container)
        return! Async.Parallel computations }

    type Microsoft.FSharp.Control.Async with
       static member StartCancelable(op:Async<'a>) (tee:'a -> unit)(?onCancel)=
            let ct = new System.Threading.CancellationTokenSource()
            let onCancel = defaultArg onCancel ignore
            Async.StartWithContinuations(op, tee, ignore, onCancel, ct.Token)
            { new IDisposable with
                member x.Dispose() = ct.Cancel() }


    let cancelOperation() =
        downloadMediaCompAsyncParallel()
        |> Async.StartCancelable


    // Async.Ignore
    let computation() = async {
        use client = new  WebClient()
        let! manningSite =
             client.AsyncDownloadString(Uri("http://www.manning.com"))
        printfn "Size %d" manningSite.Length
        return manningSite
    }
    Async.Ignore (computation())  |> Async.Start


    // Async.StartWithContinuations
    Async.StartWithContinuations(computation(),
        (fun site-> printfn "Size %d" site.Length),
        (fun exn->printfn"exception-%s"<|exn.ToString()),
        (fun exn->printfn"cancell-%s"<|exn.ToString()))


    // Async.Start
    let computationUnit() = async {
        do! Async.Sleep 1000
        use client = new WebClient()
        let! manningSite =
             client.AsyncDownloadString(Uri("http://www.manning.com"))
        printfn "Size %d" manningSite.Length
    }
    Async.Start(computationUnit())


    let getCloudBlobContainer() : CloudBlobContainer =
        let storageAccount = CloudStorageAccount.Parse(azureConnection)
        let blobClient = storageAccount.CreateCloudBlobClient()
        let container = blobClient.GetContainerReference("media")
        let _ = container.CreateIfNotExists()
        container

    // Cancellation of an asynchronous computation
    let tokenSource = new CancellationTokenSource()

    let container = getCloudBlobContainer()
    let parallelComp() =
        container.ListBlobs()
        |> Seq.map(fun blob -> downloadMediaCompAsync container blob)
        |> Async.Parallel

    Async.Start(parallelComp() |> Async.Ignore, tokenSource.Token)
    tokenSource.Cancel()


    // Cancellation of asynchronous computation with notification
    let onCancelled = fun (cnl:OperationCanceledException) ->
                        printfn "Operation cancelled!"

    //let tokenSource = new CancellationTokenSource()
    let tryCancel = Async.TryCancelled(parallelComp(), onCancelled)
    Async.Start(tryCancel |> Async.Ignore, tokenSource.Token)


    // Async.RunSynchronously
    let computation'() = async {
        do! Async.Sleep 1000
        use client = new  WebClient()
        return! client.AsyncDownloadString(Uri("www.manning.com"))
        }
    let manningSite = Async.RunSynchronously(computation'())
    printfn "Size %d" manningSite.Length



    // ParallelWithThrottle and ParallelWithCatchThrottle
    type Result<'a> = Result<'a, exn>

    module Result =
        let ofChoice value =
            match value with
            | Choice1Of2 value -> Ok value
            | Choice2Of2 e -> Error e

    module Async =
        let parallelWithCatchThrottle (selector:Result<'a> -> 'b)
                                      (throttle:int)
                                      (computations:seq<Async<'a>>) = async {
            use semaphore = new SemaphoreSlim(throttle)
            let throttleAsync (operation:Async<'a>) = async {
                try
                    do! semaphore.WaitAsync()
                    let! result = Async.Catch operation
                    return selector (result |> Result.ofChoice)
                finally
                    semaphore.Release() |> ignore }
            return! computations
                    |> Seq.map throttleAsync
                    |> Async.Parallel
        }

        let parallelWithThrottle throttle computations =
            parallelWithCatchThrottle id throttle computations


    // ParallelWithThrottle in action with Azure Table Storage downloads
    let maxConcurrentOperations = 100
    ServicePointManager.DefaultConnectionLimit <- maxConcurrentOperations

    let downloadMediaCompAsyncParallelThrottle() = async {
        let! container = getCloudBlobContainerAsync()
        let computations =
          container.ListBlobs()
          |> Seq.map(fun blobMedia -> transormAndSaveImage container blobMedia)

        return! Async.parallelWithThrottle

    maxConcurrentOperations computations }