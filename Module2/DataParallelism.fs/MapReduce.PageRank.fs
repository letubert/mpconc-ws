module MapReduce.Task

open Paket

//  PageRank object encapsulates the Map and Reduce functions
type PageRank (ranks:seq<string*float>) =
    let map = Map.ofSeq ranks
    let getRank package =
        match map.TryFind package with
        | Some(rank) -> rank
        | None -> 1.0

    member this.Map (package:NuGet.NuGetPackageCache) =
        let score =
            (getRank package.PackageName) / float(package.Dependencies.Length)
        package.Dependencies
        |> Seq.map (fun (Domain.PackageName(name,_),_,_) -> (name, score))

    member this.Reduce (name:string) (values:seq<float>) =
        name, Seq.sum values
