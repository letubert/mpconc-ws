﻿module AsyncModule

open System.IO
open System.Net

let sites =
    [   "http://www.live.com";      "http://www.fsharp.org";
        "http://news.live.com";     "http://www.digg.com";
        "http://www.yahoo.com";     "http://www.amazon.com"
        "http://news.yahoo.com";    "http://www.microsoft.com";
        "http://www.google.com";    "http://www.netflix.com";
        "http://news.google.com";   "http://www.maps.google.com";
        "http://www.bing.com";      "http://www.microsoft.com";
        "http://www.facebook.com";  "http://www.docs.google.com";
        "http://www.youtube.com";   "http://www.gmail.com";
        "http://www.reddit.com";    "http://www.twitter.com";   ]

// TODO : 4.11
// run in parallel
//  Parallel Asynchronous computations
let httpAsync (url : string) = async {
    let req = WebRequest.Create(url)
    let! resp = req.AsyncGetResponse()
    use stream = resp.GetResponseStream()
    use reader = new StreamReader(stream)
    let! text = reader.ReadToEndAsync() |> Async.AwaitTask
    return text
}

let runAsync () =
    sites
    |> Seq.map httpAsync
    |> Async.Parallel
    |> Async.RunSynchronously

let gate = RequestGate.RequestGate(2)

let httpAsyncThrottle (url : string) = async {
    let req = WebRequest.Create(url)
    use! g = gate.Aquire()
    let! resp = req.AsyncGetResponse()
    use stream = resp.GetResponseStream()
    use reader = new StreamReader(stream)
    let! text = reader.ReadToEndAsync() |> Async.AwaitTask
    return text
}

let runAsyncThrottle () =
    sites
    |> Seq.map httpAsync
    |> Async.Parallel
    |> Async.RunSynchronously

let httpSync (url:string) =
    let req = WebRequest.Create(url)
    let resp = req.GetResponse()
    use stream = resp.GetResponseStream()
    use reader = new StreamReader(stream)
    let text = reader.ReadToEnd()
    text

let runSync () =
    sites
    |> List.map httpSync