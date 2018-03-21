using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Functional.Tasks
{
    public static partial class TaskEx
    {
        public static Task<T> OrElse<T>(this Task<T> task, Func<Task<T>> fallback)
            => task.ContinueWith(t =>
                    t.Status == TaskStatus.Faulted
                    ? fallback()
                    : Task.FromResult(t.Result)
                )
                .Unwrap();


        public static Task<T> Recover<T>(this Task<T> task, Func<Exception, T> fallback)
           => task.ContinueWith(t =>
                 t.Status == TaskStatus.Faulted
                    ? fallback(t.Exception)
                    : t.Result);

        public static Task<T> RecoverWith<T>(this Task<T> task, Func<Exception, Task<T>> fallback)
           => task.ContinueWith(t =>
                 t.Status == TaskStatus.Faulted
                    ? fallback(t.Exception)
                    : Task.FromResult(t.Result)
           ).Unwrap();

        public static Task<T> Catch<T, TError>(this Task<T> task, Func<TError, T> onError) where TError : Exception
        {
            var tcs = new TaskCompletionSource<T>();
            task.ContinueWith(innerTask =>
            {
                if (innerTask.IsFaulted && innerTask?.Exception?.InnerException is TError)
                    tcs.SetResult(onError((TError)innerTask.Exception.InnerException));
                else if (innerTask.IsCanceled)
                    tcs.SetCanceled();
                else if (innerTask.IsFaulted)
                    tcs.SetException(innerTask?.Exception?.InnerException ?? throw new InvalidOperationException());
                else
                    tcs.SetResult(innerTask.Result);
            });
            return tcs.Task;
        }

        // asynchronous lift functions
        public static Task<TOut> LifTMid<TIn, TMid, TOut>(Func<TIn, TMid, TOut> selector, Task<TIn> item1, Task<TMid> item2)
        {
            // Func<TIn, Func<TMid, R>> curry = x => y => selector(x, y);

            var lifted1 = Pure(Functional.Curry(selector));
            var lifted2 = Apply(item1, lifted1);
            return Apply(item2, lifted2);
        }

        public static Task<TOut> LifTOut<TIn, TMid1, TMid2, TOut>(Func<TIn, TMid1, TMid2, TOut> selector, Task<TIn> item1, Task<TMid1> item2, Task<TMid2> item3)
        {
            //Func<TIn, Func<TMid1, Func<TMid2, TOut>>> curry = x => y => z => selector(x, y, z);

            var lifted1 = Pure(Functional.Curry(selector));
            var lifted2 = Apply(item1, lifted1);
            var lifted3 = Apply(item2, lifted2);
            return Apply(item3, lifted3);
        }

        public static Task<TOut> Fmap<TIn, TOut>(this Task<TIn> input, Func<TIn, TOut> map) => input.ContinueWith(t => map(t.Result));

        public static Task<TOut> Map<TIn, TOut>(this Task<TIn> input, Func<TIn, TOut> map) => input.ContinueWith(t => map(t.Result));

        public static Task<T> Return<T>(this T input) => Task.FromResult(input);

        public static Task<T> Pure<T>(T input) => Task.FromResult(input);

        public static Task<TOut> Apply<TIn, TOut>(this Task<TIn> task, Task<Func<TIn, TOut>> liftedFn)
        {
            var tcs = new TaskCompletionSource<TOut>();
            liftedFn.ContinueWith(innerLiftTask =>
                task.ContinueWith(innerTask =>
                    tcs.SetResult(innerLiftTask.Result(innerTask.Result))
            ));
            return tcs.Task;
        }

        public static Task<TOut> Apply<TIn, TOut>(this Task<Func<TIn, TOut>> liftedFn, Task<TIn> task) => task.Apply(liftedFn);

        public static Task<Func<TMid, TOut>> Apply<TIn, TMid, TOut>(this Task<Func<TIn, TMid, TOut>> liftedFn, Task<TIn> input)
            => input.Apply(liftedFn.Fmap(Functional.Curry));

        public static Task<TOut> Bind<TIn, TOut>(this Task<TIn> input, Func<TIn, Task<TOut>> f)
        {
            var tcs = new TaskCompletionSource<TOut>();
            input.ContinueWith(x =>
                f(x.Result).ContinueWith(y =>
                    tcs.SetResult(y.Result)));
            return tcs.Task;
        }

        public static IEnumerable<Task<T>> ProcessAsComplete<T>(this IEnumerable<Task<T>> inputTasks)
        {
            // Copy the input so we know it’ll be stable, and we don’t evaluate it twice
            var inputTaskList = inputTasks.ToList();
            // Could use Enumerable.Range here, if we wanted…
            var completionSourceList = new List<TaskCompletionSource<T>>(inputTaskList.Count);
            for (int i = 0; i < inputTaskList.Count; i++)
            {
                completionSourceList.Add(new TaskCompletionSource<T>());
            }

            // At any one time, this is "the index of the box we’ve just filled".
            // It would be nice to make it nextIndex and start with 0, but Interlocked.Increment
            // returns the incremented value…
            int prevIndex = -1;

            // We don’t have to create this outside the loop, but it makes it clearer
            // that the continuation is the same for all tasks.
            Action<Task<T>> continuation = completedTask =>
            {
                int index = Interlocked.Increment(ref prevIndex);
                var source = completionSourceList[index];
                switch (completedTask.Status)
                {
                    case TaskStatus.Canceled:
                        source.TrySetCanceled();
                        break;
                    case TaskStatus.Faulted:
                        source.TrySetException(completedTask.Exception.InnerExceptions);
                        break;
                    case TaskStatus.RanToCompletion:
                        source.TrySetResult(completedTask.Result);
                        break;
                    default:
                        throw new ArgumentException("Task was not completed");
                }
            };

            foreach (var inputTask in inputTaskList)
            {
                inputTask.ContinueWith(continuation,
                                       CancellationToken.None,
                                       TaskContinuationOptions.ExecuteSynchronously,
                                       TaskScheduler.Default);
            }

            return completionSourceList.Select(source => source.Task);
        }

        public static Task<IEnumerable<T>> Traverese<T>(this IEnumerable<Task<T>> sequence)
        {
            return sequence.Aggregate(
                Task.FromResult(Enumerable.Empty<T>()),
                (eventualAccumulator, eventualItem) =>
                    from accumulator in eventualAccumulator
                    from item in eventualItem
                    select accumulator.Concat(new[] { item }).ToArray().AsEnumerable());
        }

        public static Func<T, Task<U>> Kleisli<T, R, U>(Func<T, Task<R>> task1, Func<R, Task<U>> task2) =>
            value => task1(value).Bind(task2);
    }
}


