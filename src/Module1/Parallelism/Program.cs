using ParallelizingFuzzyMatch;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DataParallelism.cs
{
    class Program
    {
        static void TestFuzzyMatch()
        {
            Demo.PrintSeparator();
            Console.WriteLine("Fuzzy Match");
            Func<Action, Action[]> run = (func) => new Action[] { () => { func(); } };

            var fuzzyMatchImplementations =
                new[]
                {
                    new Tuple<String, Action[]>(
                        "C# Sequential FuzzyMatch", run(ProcessFuzzyMacth.LinqFuzzyMatch)),
                    new Tuple<String, Action[]>(
                        "C# Multiple Tasks FuzzyMatch", run(ProcessFuzzyMacth.MultipleTasksFuzzyMatch)),
                    new Tuple<String, Action[]>(
                        "C# Parallel Loop FuzzyMatch", run(ProcessFuzzyMacth.ParallelLoopFuzzyMatch)),
                    new Tuple<String, Action[]>(
                        "C# Parallel FuzzyMatch", run(ProcessFuzzyMacth.ParallelLinqFuzzyMatch)),
                        new Tuple<String, Action[]>(
                        "C# Parallel PLINQ Partitioner FuzzyMatch", run(ProcessFuzzyMacth.ParallelLinqPartitionerFuzzyMatch))
                };
            Application.Run(
                PerfVis.toChart("C# FuzzyMatch")
                    .Invoke(PerfVis.fromTuples(fuzzyMatchImplementations)));
        }

        static void TestMandelbrot()
        {

            Console.WriteLine("Mandelbrot Performance Comparison");
            Func<Func<Bitmap>, Action[]> run = (func) =>
                    new Action[] { () => { func(); } };

            var implementations =
                new[]
                {
                    new Tuple<String, Action[]>(
                        "C# Sequential", run(Mandelbrot.SequentialMandelbrot)),
                    new Tuple<String, Action[]>(
                        "C# Parallel.For", run(Mandelbrot.ParallelMandelbrot)),
                    new Tuple<String, Action[]>(
                        "C# Parallel.For Saturated", run(Mandelbrot.ParallelMandelbrotOversaturation)),
                    new Tuple<String, Action[]>(
                        "C# Parallel.For Struct", run(Mandelbrot.ParallelStructMandelbrot))
                };

            Application.Run(
                PerfVis.toChart("C# Mandelbrot")
                    .Invoke(PerfVis.fromTuples(implementations)));
        }

        static void TestPrimeNumbersSum()
        {
            Demo.PrintSeparator();
            Console.WriteLine("Prime Sum [0..10^7]");
            Func<Func<long>, Action[]> runSum = (func) =>
                new Action[]
                {
                    () =>
                    {
                        var result = func();
                        Console.WriteLine($"Sum = {result}");
                    }
                };

            var sumImplementations =
                new[]
                {
                    new Tuple<String, Action[]>(
                        "C# Sequential", runSum(PrimeNumbers.PrimeSumSequential)),
                    new Tuple<String, Action[]>(
                        "C# Parallel.For", runSum(PrimeNumbers.PrimeSumParallel)),
                    new Tuple<String, Action[]>(
                        "C# Parallel.For ThreadLocal", runSum(PrimeNumbers.PrimeSumParallelThreadLocal)),
                    new Tuple<String, Action[]>(
                        "C# Parallel LINQ", runSum(PrimeNumbers.PrimeSumParallelLINQ))
                };
            Application.Run(
                PerfVis.toChart("C# Prime Sum")
                    .Invoke(PerfVis.fromTuples(sumImplementations)));
        }
        static void Main(string[] args)
        {
            TestFuzzyMatch();
            //TestMandelbrot();
            //TestPrimeNumbersSum();
            Console.WriteLine("Completed");
            Console.ReadLine();

        }
    }
}
