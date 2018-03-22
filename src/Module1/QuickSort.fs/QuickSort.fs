module QuickSort

open System
open System.Threading.Tasks

type ParallelismHelpers =
    static member MaxDepth =
        int (Math.Log(float Environment.ProcessorCount, 2.0))

    static member TotalWorkers =
        int (2.0 ** float (ParallelismHelpers.MaxDepth))


//  A simple Quick-sort Algorithm
let rec quicksortSequential aList =
    match aList with
    | [] -> []
    | firstElement :: restOfList ->
        let smaller, larger =
            List.partition (fun number -> number > firstElement) restOfList
        quicksortSequential smaller @ (firstElement :: quicksortSequential larger)


//  A parallel Quick-Sort Algorithm using the TPL library
let rec quicksortParallel aList =
    match aList with
    | [] -> []
    | firstElement :: restOfList ->
        let smaller, larger =
            List.partition (fun number -> number > firstElement) restOfList
        let left  = Task.Run(fun () -> quicksortParallel smaller)
        let right = Task.Run(fun () -> quicksortParallel larger)
        left.Result @ (firstElement :: right.Result)

// TODO : 1.5
// write a parallel and fast quick sort
// A better parallel Quick-Sort Algorithm using the TPL library
let rec fasterQuicksortParallel aList = ()

// Solution
// better parallel Quick-Sort Algorithm using the TPL library
let rec quicksortParallelWithDepth depth aList =
    match aList with
    | [] -> []
    | firstElement :: restOfList ->
        let smaller, larger =
            List.partition (fun number -> number > firstElement) restOfList
        if depth < 0 then
            let left  = quicksortParallelWithDepth depth smaller
            let right = quicksortParallelWithDepth depth larger
            left @ (firstElement :: right)
        else
            let left  = Task.Run(fun () -> quicksortParallelWithDepth (depth - 1) smaller)
            let right = Task.Run(fun () -> quicksortParallelWithDepth (depth - 1) larger)
            left.Result @ (firstElement :: right.Result)