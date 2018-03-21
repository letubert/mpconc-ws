// Helper file used in demos, not directly part of demo

#r "System.Core.dll"
open System
open System.Linq

type pseq<'a> = ParallelQuery<'a>

let toParallel (s : seq<'a>) =
    match s with
    | :? pseq<'a> as p ->  p
    | _ -> s.AsParallel()

// Remaining functions of Seq module can be mapped to PSeq versions using PLinq
// The full library is planned for a future F# PowerPack release, but see also:

