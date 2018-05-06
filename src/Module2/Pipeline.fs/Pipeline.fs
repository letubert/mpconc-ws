namespace Pipeline

open System
open System.Linq
open System.Collections.Concurrent
open System.Threading
open System.Threading.Tasks

//  The IPipeline interface
[<Interface>]
type IPipeline<'a,'b> =
    abstract member Then : ('b -> 'c) -> IPipeline<'a,'c>
    abstract member Enqueue : 'a * (('a * 'b) -> unit) -> unit
    abstract member Execute : int * CancellationToken -> IDisposable
    abstract member Stop : unit -> unit


// The Parallel Functional Pipeline pattern
[<Struct>]
type internal Continuation<'a, 'b>(input:'a, callback:('a * 'b) -> unit) =
    member this.Input with get() = input
    member this.Callback with get() = callback

type Pipeline<'a, 'b> private (func:'a -> 'b) as this =
    let continuations = Array.init 3 (fun _ -> new BlockingCollection<Continuation<'a,'b>>(100))

    // TODO : 2.16
    // implement a then' function that composes a given function to the current "func" one
    // Suggestion, use the ">>" operator

    // let then'  ...

    // #region solution
    let then' (nextFunction:'b -> 'c) =
        Pipeline(func >> nextFunction) :> IPipeline<_,_>
    // #endregion

    let enqueue (input:'a) (callback:('a * 'b) -> unit) =
        BlockingCollection<Continuation<_,_>>.AddToAny(continuations, Continuation(input, callback))

    let stop() = for continuation in continuations do continuation.CompleteAdding()

    let execute blockingCollectionPoolSize (cancellationToken:CancellationToken) =

        cancellationToken.Register(Action(stop)) |> ignore

        for i = 0 to blockingCollectionPoolSize - 1 do
            Task.Factory.StartNew(fun ( )->
                while (not <| continuations.All(fun bc -> bc.IsCompleted)) && (not <| cancellationToken.IsCancellationRequested) do
                    let continuation = ref Unchecked.defaultof<Continuation<_,_>>
                    BlockingCollection.TakeFromAny(continuations, continuation) |> ignore
                    let continuation = continuation.Value
                    continuation.Callback(continuation.Input, func(continuation.Input))

            ,cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default) |> ignore

    static member Create(func:'a -> 'b) =
        Pipeline(func) :> IPipeline<_,_>

    interface IPipeline<'a, 'b> with
        // TODO : 2.16 remove "Unchecked.defaultof<IPipeline<_,_>>"  and apply the new function created, for example "then' nextFunction"
        member this.Then(nextFunction) = Unchecked.defaultof<IPipeline<_,_>> // then' nextFunction

        member this.Enqueue(input, callback) = enqueue input callback |> ignore
        member this.Stop() = stop()
        member this.Execute (blockingCollectionPoolSize, cancellationToken) =
            execute blockingCollectionPoolSize cancellationToken
            { new IDisposable with member self.Dispose() = stop() }
