[<AutoOpen>]
module ParallelWorkersAgent

open System
open System.Threading

type Agent<'a> = MailboxProcessor<'a>

// Demo 
// Parallel MailboxProcessor workers
type MailboxProcessor<'a> with
    static member public parallelWorker' (workers:int)
            (behavior:MailboxProcessor<'a> -> Async<unit>)
            (?errorHandler:exn -> unit) (?cts:CancellationToken) =

        let cts = defaultArg cts (CancellationToken())
        let errorHandler = defaultArg errorHandler ignore
        let agent = new MailboxProcessor<'a>((fun inbox ->
            let agents = Array.init workers (fun _ ->
                let child = MailboxProcessor.Start(behavior, cts)
                child.Error.Subscribe(errorHandler) |> ignore
                child)
            cts.Register(fun () ->
                agents |> Array.iter(
                    fun a -> (a :> IDisposable).Dispose()))
            |> ignore

            let rec loop i = async {
                let! msg = inbox.Receive()
                agents.[i].Post(msg)
                return! loop((i+1) % workers)
            }
            loop 0), cts)
        agent.Start()



type AgentDisposable<'T>(f:MailboxProcessor<'T> -> Async<unit>,
                            ?cancelToken:System.Threading.CancellationTokenSource) =
    let cancelToken = defaultArg cancelToken (new CancellationTokenSource())
    let agent = MailboxProcessor.Start(f, cancelToken.Token)

    member x.Agent = agent
    interface IDisposable with
        member x.Dispose() = (agent :> IDisposable).Dispose()
                             cancelToken.Cancel()

type AgentDisposable<'T> with
    member inline this.withSupervisor (supervisor: Agent<exn>, transform) =
        this.Agent.Error.Add(fun error -> supervisor.Post(transform(error))); this

    member this.withSupervisor (supervisor: Agent<exn>) =
        this.Agent.Error.Add(supervisor.Post); this


type MailboxProcessor<'a> with
    static member public parallelWorker (workers:int, behavior:MailboxProcessor<'a> -> Async<unit>, ?errorHandler, ?cancelToken:CancellationTokenSource) =
        let cancelToken = defaultArg cancelToken (new System.Threading.CancellationTokenSource())
        let thisletCancelToken = cancelToken.Token
        let errorHandler = defaultArg errorHandler ignore
        let supervisor = Agent<System.Exception>.Start(fun inbox -> async {
                            while true do
                                let! error = inbox.Receive()
                                errorHandler error })
        let agent = new MailboxProcessor<'a>((fun inbox ->
            let agents = Array.init workers (fun _ ->
                (new AgentDisposable<'a>(behavior, cancelToken))
                    .withSupervisor supervisor )
            thisletCancelToken.Register(fun () ->
                agents |> Array.iter(fun agent -> (agent :> IDisposable).Dispose())
            ) |> ignore
            let rec loop i = async {
                let! msg = inbox.Receive()
                agents.[i].Agent.Post(msg)
                return! loop((i+1) % workers)
            }
            loop 0), thisletCancelToken)
        agent.Start()
        agent
