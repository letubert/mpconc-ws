using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Functional.Tasks
{
    public static partial class TaskEx
    {
        public static Task<TOut> Select_TODO<TIn, TOut>(this Task<TIn> task, Func<TIn, TOut> projection)
        {
            // TODO : 2.9
            // implement the body of this function starting from the signature type
            return null;
        }


        public static Task<TOut> SelectMany_TODO<TIn, TOut>(this Task<TIn> first, Func<TIn, Task<TOut>> next)
        {
            // TODO : 2.9
            // implement the body of this function starting from the signature type
            return null;
        }

        public static Task<TOut> SelectMany_TODO<TIn, TMid, TOut>(
          this Task<TIn> input, Func<TIn, Task<TMid>> f, Func<TIn, TMid, TOut> projection)
        {
            // TODO : 2.9
            // implement the body of this function starting from the signature type
            return null;
        }

        #region Solution
        public static Task<TOut> Select<TIn, TOut>(this Task<TIn> task, Func<TIn, TOut> projection)
        {
            var r = new TaskCompletionSource<TOut>();
            task.ContinueWith(self =>
            {
                if (self.IsFaulted) r.SetException(self.Exception.InnerExceptions);
                else if (self.IsCanceled) r.SetCanceled();
                else r.SetResult(projection(self.Result));
            });
            return r.Task;
        }


        public static Task<TOut> SelectMany<TIn, TOut>(this Task<TIn> first, Func<TIn, Task<TOut>> next)
        {
            var tcs = new TaskCompletionSource<TOut>();
            first.ContinueWith(delegate
            {
                if (first.IsFaulted) tcs.TrySetException(first.Exception.InnerExceptions);
                else if (first.IsCanceled) tcs.TrySetCanceled();
                else
                {
                    try
                    {
                        var t = next(first.Result);
                        if (t == null) tcs.TrySetCanceled();
                        else t.ContinueWith(delegate
                        {
                            if (t.IsFaulted) tcs.TrySetException(t.Exception.InnerExceptions);
                            else if (t.IsCanceled) tcs.TrySetCanceled();
                            else tcs.TrySetResult(t.Result);
                        }, TaskContinuationOptions.ExecuteSynchronously);
                    }
                    catch (Exception exc) { tcs.TrySetException(exc); }
                }
            }, TaskContinuationOptions.ExecuteSynchronously);
            return tcs.Task;
        }

        public static Task<TOut> SelectMany<TIn, TMid, TOut>(
          this Task<TIn> input, Func<TIn, Task<TMid>> f, Func<TIn, TMid, TOut> projection)
        {
            return Bind(input, outer =>
                   Bind(f(outer), inner =>
                   Return(projection(outer, inner))));
        }
        #endregion
    }
}