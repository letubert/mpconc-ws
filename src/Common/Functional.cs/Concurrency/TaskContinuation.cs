using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Functional.Tasks
{
    public static partial class TaskEx
    {
        public static Task<TOut> Select<TIn, TOut>(this Task<TIn> task, Func<TIn, TOut> projection)
        {
            // TODO : 2.9
            // implement the body of this function starting from the signature type
            return null;
        }


        public static Task<TOut> SelectMany<TIn, TOut>(this Task<TIn> first, Func<TIn, Task<TOut>> next)
        {
            // TODO : 2.9
            // implement the body of this function starting from the signature type
            return null;
        }

        public static Task<TOut> SelectMany<TIn, TMid, TOut>(
          this Task<TIn> input, Func<TIn, Task<TMid>> f, Func<TIn, TMid, TOut> projection)
        {
            // TODO : 2.9
            // implement the body of this function starting from the signature type
            return null;
        }

    }
}