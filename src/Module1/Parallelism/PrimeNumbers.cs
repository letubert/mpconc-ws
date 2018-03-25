using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataParallelism.cs
{
    public static class PrimeNumbers
    {
        public static long PrimeSumSequential()
        {
            int len = 10000000;
            long total = 0;
            Func<int, bool> isPrime = n =>
            {
                if (n == 1) return false;
                if (n == 2) return true;
                var boundary = (int)Math.Floor(Math.Sqrt(n));
                for (int i = 2; i <= boundary; ++i)
                    if (n % i == 0) return false;
                return true;
            };

            for (var i = 1; i <= len; ++i)
            {
                if (isPrime(i))
                    total += i;
            }
            return total;
        }

        public static long PrimeSumParallel()
        {
            // Parallel sum of prime numbers in a collection using Parallel.For loop construct
            int len = 10000000;
            long total = 0;
            Func<int, bool> isPrime = n =>
            {
                if (n == 1) return false;
                if (n == 2) return true;
                var boundary = (int)Math.Floor(Math.Sqrt(n));
                for (int i = 2; i <= boundary; ++i)
                    if (n % i == 0) return false;
                return true;
            };

            // WRONG!
            Parallel.For(0, len, i =>
            {
                if (isPrime(i))
                    total += i;
            });
            return total;
        }



        // TODO : 1.6
        // Write a parallel PrimeSum method
        // using Parallel For loop
        // avoid race-conditions
        // suggestion, the name of the method might help
        public static long PrimeSumParallelThreadLocal_TODO()
        {
            int len = 10000000;
            long total = 0;
            Func<int, bool> isPrime = n =>
            {
                if (n == 1) return false;
                if (n == 2) return true;
                var boundary = (int)Math.Floor(Math.Sqrt(n));
                for (int i = 2; i <= boundary; ++i)
                    if (n % i == 0) return false;
                return true;
            };

            // TODO
            //Parallel.For(0, len, i =>

            return total;
        }
        // TODO : 1.7
        // Write a parallel PrimeSum method using PLINQ
        public static long PrimeSumParallelLINQ_TODO()
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

            // Parallel sum of a collection using declarative PLINQ
            long total = 0L;
            return total;
        }


        #region Solution
        public static long PrimeSumParallelThreadLocal()
        {
            int len = 10000000;
            long total = 0;
            Func<int, bool> isPrime = n =>
            {
                if (n == 1) return false;
                if (n == 2) return true;
                var boundary = (int)Math.Floor(Math.Sqrt(n));
                for (int i = 2; i <= boundary; ++i)
                    if (n % i == 0) return false;
                return true;
            };

            // Thread-safe parallel sum using Parallel.For and ThreadLocal
            Parallel.For(0, len,
                () => 0,
                (int i, ParallelLoopState loopState, long tlsValue)
                    => isPrime(i) ? tlsValue += i : tlsValue,
                value => Interlocked.Add(ref total, value));

            return total;
        }

        public static long PrimeSumParallelLINQ()
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

            // Parallel sum of a collection using declarative PLINQ
            long total = Enumerable.Range(0, len).AsParallel().Where(isPrime).Sum(x => (long)x);
            return total;
        }
        #endregion

    }
}