using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace DataParallelism.CSharp
{
    using static Functional.Functional;

    // TODO : Implement a Map-Reduce Function (as extension method - reusable)
    public static class MapReducePLINQ_TODO
    {
        // (1)
        // start with Map, follow this method signature
        // The IGrouping is achieved with the keySelector function, this is arbitrary and you can implement the Map function without it
        public static IEnumerable<IGrouping<TKey, TMapped>> Map<TSource, TKey, TMapped>(this IList<TSource> source,
            Func<TSource, IEnumerable<TMapped>> map, Func<TMapped, TKey> keySelector)
        {
            // replace null with the implementation
            return null;
        }

        // (2)
        // Implement the reduce function, this is a suggested signature but you can simplified it and/or expanded it
        public static TResult[] Reduce<TSource, TKey, TMapped, TResult>(
            this IEnumerable<IGrouping<TKey, TMapped>> source, Func<IGrouping<TKey, TMapped>, TResult> reduce)
        {
            // replace null with the implementation
            return null;
        }

        // (3)
        // Compose the pre-implemented Map and Reduce function
        // After the implementation of the Map-Reduce
        // - How can you control/manage the degree of parallelism ?
        // - Improve the performance with a Partitioner
        // Suggestion, for performance improvement look into the "WithExecutionMode"
        public static TResult[] MapReduce<TSource, TMapped, TKey, TResult>(this IList<TSource> source,
            Func<TSource, IEnumerable<TMapped>> map, Func<TMapped, TKey> keySelector,
            Func<IGrouping<TKey, TMapped>, TResult> reduce)
        {
            return null;
        }

         public static TResult[] MapReduce<TSource, TMapped, TKey, TResult>(
            this IList<TSource> source,
            Func<TSource, IEnumerable<TMapped>> map,
            Func<TMapped, TKey> keySelector,
            Func<IGrouping<TKey, TMapped>, TResult> reduce,
            int M, int R)
        {
              return null;
        }
    }

    #region Solution
    public static class MapReducePLINQ
    {
        public static IEnumerable<IGrouping<TKey, TMapped>> Map<TSource, TKey, TMapped>(
            this IList<TSource> source, Func<TSource, IEnumerable<TMapped>> map, Func<TMapped, TKey> keySelector) =>
            source.AsParallel()
                //.WithDegreeOfParallelism(Environment.ProcessorCount)
                .SelectMany(map)
                .GroupBy(keySelector)
                .ToList();

        public static TResult[] Reduce<TSource, TKey, TMapped, TResult>(
            this IEnumerable<IGrouping<TKey, TMapped>> source,
            Func<IGrouping<TKey, TMapped>, TResult> reduce) =>
            source.AsParallel()
                //   .WithDegreeOfParallelism(Environment.ProcessorCount)
                .Select(reduce).ToArray();

        public static TResult[] MapReduce<TSource, TMapped, TKey, TResult>(
            this IList<TSource> source,
            Func<TSource, IEnumerable<TMapped>> map,
            Func<TMapped, TKey> keySelector,
            Func<IGrouping<TKey, TMapped>, TResult> reduce)
        {
            return Map(source, map, keySelector).AsParallel().Select(reduce).ToArray();
        }

        public static TResult[] MapReduce<TSource, TMapped, TKey, TResult>(
            this IList<TSource> source,
            Func<TSource, IEnumerable<TMapped>> map,
            Func<TMapped, TKey> keySelector,
            Func<IGrouping<TKey, TMapped>, TResult> reduce,
            int M, int R)
        {
            var partitioner = Partitioner.Create(source, true);
            return partitioner.AsParallel()
                .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                .WithDegreeOfParallelism(M)
                .SelectMany(map)
                .GroupBy(keySelector)
                .ToList().AsParallel()
                .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                .WithDegreeOfParallelism(R)
                .Select(reduce)
                .ToArray();
        }

        public static TResult[] MapReduceFunc<TSource, TMapped, TKey, TResult>(
            this IList<TSource> source,
            Func<TSource, IEnumerable<TMapped>> map,
            Func<TMapped, TKey> keySelector,
            Func<IGrouping<TKey, TMapped>, TResult> reduce,
            int M, int R)
        {
            //var m = Map(source, map, keySelector);
            //var r = Reduce<TSource, TKey, TMapped, TResult>(m, reduce);

            Func<IList<TSource>, Func<TSource, IEnumerable<TMapped>>, Func<TMapped, TKey>,
                IEnumerable<IGrouping<TKey, TMapped>>> mapFunc = Map;
            Func<IEnumerable<IGrouping<TKey, TMapped>>, Func<IGrouping<TKey, TMapped>, TResult>, TResult[]> reduceFunc =
                Reduce<TSource, TKey, TMapped, TResult>;

            Func<IList<TSource>, Func<TSource, IEnumerable<TMapped>>, Func<TMapped, TKey>,
                Func<IGrouping<TKey, TMapped>, TResult>, TResult[]> mapReduceFunc = mapFunc.Compose(reduceFunc);

            return mapReduceFunc(source, map, keySelector, reduce);
        }

        public static Func<Func<TSource, IEnumerable<TMapped>>, Func<TMapped, TKey>, Func<IGrouping<TKey, TMapped>, TResult>, TResult[]> MapReduceFunc<TSource, TMapped, TKey, TResult>(
            this IList<TSource> source)
        {
            Func<IList<TSource>, Func<TSource, IEnumerable<TMapped>>, Func<TMapped, TKey>, IEnumerable<IGrouping<TKey, TMapped>>> mapFunc = Map;
            Func<IEnumerable<IGrouping<TKey, TMapped>>, Func<IGrouping<TKey, TMapped>, TResult>, TResult[]> reduceFunc = Reduce<TSource, TKey, TMapped, TResult>;

            return mapFunc.Compose(reduceFunc).Partial(source);
        }


        #endregion
    }
}