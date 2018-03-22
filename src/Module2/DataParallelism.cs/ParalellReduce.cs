using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonHelpers;
using DataParallelism.CSharp;

namespace DataParallelism.CSharp
{
    using static Functional.Functional;

    // TODO : 2.2
    // implement two parallel Reducer functions
    // requirements
    // 1 - reduce all the items in a collection starting from the first one
    // 3 - reduce all the items in a collection starting from a given initial value
    // Suggestion, look into the LINQ Aggregate
    // You could implement two different functions with different signatures
    // Tip : there are different ways to implement a parallel reducer, even using a Parallel For loop
    public static class ParalellReduce
    {
        // public static TValue Reduce<TValue>

        // parallel Reduce function implementation using Aggregate
        // Example of signature, but something is missing
        // public static TValue Reduce<TValue>(this IEnumerable<TValue> source) =>
        public static TValue Reduce<TValue>(this ParallelQuery<TValue> source, Func<TValue, TValue, TValue> func) => default(TValue);

         public static TValue Reduce<TValue>(this IEnumerable<TValue> source, TValue seed,
            Func<TValue, TValue, TValue> reduce) => default(TValue);

        public static TResult[] Reduce<TSource, TKey, TMapped, TResult>(
            this IEnumerable<IGrouping<TKey, TMapped>> source, Func<IGrouping<TKey, TMapped>, TResult> reduce) => null;


        public static void SumPrimeNumber_Reducer()
        {
            int len = 10000000;
            Func<int, bool> isPrime = n =>
            {
                if (n == 1) return false;
                if (n == 2) return true;
                var boundary = (int)Math.Floor(Math.Sqrt(n));
                for (int i = 2; i <= boundary; ++i)
                    if (n % i == 0) return false;
                return true;
            };

            // Parallel sum of a collection using parallel Reducer
            BenchPerformance.Time("Parallel sum of a collection using parallel Reducer", () =>
            {
                // TODO
                // calculate the total with parallel Reducer
                //
                // Note : if the len value increases, the LINQ/PLINQ "Sum" operator does not work.
                // for example, the following code does not work. The reducer function should fix the issue
                // var total = Enumerable.Range(0, len).Where(isPrime).AsParallel().Sum();

                var total = Enumerable.Range(0, len);
                Console.WriteLine($"The total is {total}");
            }, 5);

        }
    }
}