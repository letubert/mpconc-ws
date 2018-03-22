using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.FSharp.Core;
using static Pipeline.PipelineFunc;

namespace DataParallelism.Pipelines
{
    public class CsPipeline<TInput, TOutput> : IPipeline<TInput, TOutput>
    {
        private struct Continuation
        {
            public Func<Tuple<TInput, TOutput>, Unit> Callback;
            public TInput Input;
        }

        private readonly Func<TInput, TOutput> _function;
        private BlockingCollection<Continuation>[] _continuations;

        public CsPipeline(Func<TInput, TOutput> function)
        {
            _function = function;
        }

        // TODO : 2.4
        // (1)
        // implement a "Then" functions that compose the current _function with a given "next function"
        // assuming that the return type could be different from the current one "TOutput"
        // the return type is a new Pipeline, for example (conceptually), given a function "Func<TOutput, TMapped>",
        // the new Pipeline would have signature IPipeline<TInput, TMapped

        // ... CODE HERE
          public IPipeline<TInput, TMapped> Then<TMapped>(Func<TOutput, TMapped> nextfunction) => null;


        public void Enqueue(TInput input, Func<Tuple<TInput, TOutput>, Unit> callback)
        {
            BlockingCollection<Continuation>.TryAddToAny(_continuations,
                new Continuation
                {
                    Input = input,
                    Callback = callback
                });
        }

        public void Stop()
        {
            foreach (var bc in _continuations)
                bc.CompleteAdding();
        }

        public IDisposable Execute(int blockingCollectionPoolSize, CancellationToken cancellationToken)
        {
            _continuations =
                Enumerable.Range(0, blockingCollectionPoolSize)
                    .Select(_ => new BlockingCollection<Continuation>(100))
                    .ToArray();

            cancellationToken.Register(Stop);

            for (var x = 0; x < blockingCollectionPoolSize; x++)
                Task.Factory.StartNew(() =>
                {
                    while (!_continuations.All(bc => bc.IsCompleted) && !cancellationToken.IsCancellationRequested)
                    {
                        // TODO
                        // (2)
                        // implement a statement that runs the continuation from _continuations
                        // the Take continuation must be thread safe
                        // Check the BlockingCollection API
                        // Suggestion, use the Callback function to execute the continuation retrieved
                        // SUggestion, look into the BlockingCollection class API

                        Continuation continuation;

                    }
                }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);


            return new StopPipelineDisposable(this);
        }


        private class StopPipelineDisposable : IDisposable
        {
            readonly CsPipeline<TInput, TOutput> _pipeline;

            public StopPipelineDisposable(CsPipeline<TInput, TOutput> pipeline)
            {
                _pipeline = pipeline;
            }

            public void Dispose()
            {
                _pipeline.Stop();
            }
        }
    }
}