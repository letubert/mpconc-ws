module ConsurrentSpeculation

    open System
    open System.Threading.Tasks
    open System.Collections.Generic
    open FSharp.Collections.ParallelSeq
    open System.Collections.Concurrent
    open FuzzyMatch
    open System.Linq
    open Parallelism

    let fuzzyMatch (words:string list) =
        let wordSet = new HashSet<string>(words)
        let partialFuzzyMatch word =
            query { for w in wordSet.AsParallel() do
                        select (JaroWinkler.bestMatch word w) }
            |> Seq.concat
            |> Seq.sortBy(fun x -> -x.Distance)
            |> Seq.head

        fun word -> partialFuzzyMatch word


    let words = Parallelism.Data.Words |> Array.toList
    let fastFuzzyMatch = fuzzyMatch words

    let magicFuzzyMatch = fastFuzzyMatch [| "magic" |]
    let lightFuzzyMatch = fastFuzzyMatch [| "light" |]

    // TODO : 1.10
    // (1)
    // Implement the fuzzyMatch using PSeq
    let fuzzyMatchPSeq_TODO (words:string list) = ()

    let fuzzyMatchPSeq (words:string list) =
        let wordSet = new HashSet<string>(words)
        fun word ->
            wordSet
            |> PSeq.map(fun w -> JaroWinkler.bestMatch word w)
            |> PSeq.concat
            |> PSeq.sortBy(fun x -> -x.Distance)
            |> Seq.head