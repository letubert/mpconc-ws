
module Memoization

open System
open System.Collections.Concurrent

let memoize cacheTimeSeconds (caller:string) (f: ('a -> 'b)) =
    let cacheTimes = ConcurrentDictionary<string,DateTime>()
    let cache = ConcurrentDictionary<'a, 'b>()
    fun x ->
        match cacheTimes.TryGetValue caller with
        | true, time when time < DateTime.UtcNow.AddSeconds(-cacheTimeSeconds)
            -> cache.TryRemove(x) |> ignore
        | _ -> ()
        cache.GetOrAdd(x, fun x ->
            cacheTimes.AddOrUpdate(caller, DateTime.UtcNow, fun _ _ ->DateTime.UtcNow)|> ignore
            f(x)
            )

let memoizeAsync cacheTimeSeconds (caller:string) (f: ('a -> Async<'b>)) =
    let cacheTimes = ConcurrentDictionary<string,DateTime>()
    let cache = ConcurrentDictionary<'a, System.Threading.Tasks.Task<'b>>()
    fun x ->
        match cacheTimes.TryGetValue caller with
        | true, time when time < DateTime.UtcNow.AddSeconds(-cacheTimeSeconds)
            -> cache.TryRemove(x) |> ignore
        | _ -> ()
        cache.GetOrAdd(x, fun x ->
            cacheTimes.AddOrUpdate(caller, DateTime.UtcNow, fun _ _ ->DateTime.UtcNow)|> ignore
            f(x) |> Async.StartAsTask
            ) |> Async.AwaitTask

