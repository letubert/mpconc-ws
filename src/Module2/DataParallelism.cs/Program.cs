using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommonHelpers;
using DataParallelism.Pipelines;

namespace DataParallelism.CSharp
{
    public static class Program
    {
        public static void RunPipelineWorker()
        {
			var cts = new CancellationTokenSource();
            int count = 4;
			Task.Run(() =>
			{
				if (Console.ReadKey().KeyChar == 'c')
					cts.Cancel();
			});

			var sourceArrays = new BlockingCollection<int>[count];
			for (int i = 0; i < sourceArrays.Length; i++)
			{
				sourceArrays[i] = new BlockingCollection<int>();
			}

			var filter1 = new PipelineWorker<int, decimal>
			(
				sourceArrays,
				(n) => Convert.ToDecimal(n * 0.97),
				cts.Token,
				"filter1"
			);

			var filter2 = new PipelineWorker<decimal, string>
			(
				filter1.Output,
				(s) => String.Format("--{0}--", s),
				cts.Token,
				"filter2"
			 );

			var filter3 = new PipelineWorker<string, string>
			(
				filter2.Output,
				(s) => Console.WriteLine("The final result is {0} on thread id {1}",
					s, Thread.CurrentThread.ManagedThreadId),
				cts.Token,
				"filter3"
			 );

			try
			{
				Parallel.Invoke(
					() =>
					{
						Parallel.For(0, sourceArrays.Length * 4, (j, state) =>
						{
							if (cts.Token.IsCancellationRequested)
							{
								state.Stop();
							}
							int k = BlockingCollection<int>.TryAddToAny(sourceArrays, j);
							if (k >= 0)
							{
								Console.WriteLine("added {0} to source data on thread id {1}", j, Thread.CurrentThread.ManagedThreadId);
								Thread.Sleep(TimeSpan.FromMilliseconds(100));
							}
						});
						foreach (var arr in sourceArrays)
						{
							arr.CompleteAdding();
						}
					},
					() => filter1.Run(),
					() => filter2.Run(),
					() => filter3.Run()
				);
			}
			catch (AggregateException ae)
			{
				foreach (var ex in ae.InnerExceptions)
					Console.WriteLine(ex.Message + ex.StackTrace);
			}

			if (cts.Token.IsCancellationRequested)
			{
				Console.WriteLine("Operation has been canceled! Press ENTER to exit.");
			}
			else
			{
				Console.WriteLine("Press ENTER to exit.");
			}
			Console.ReadLine();
		}

        public static void RunPipelineFilter()
        {
            //Generate the source data.
            var source = new BlockingCollection<int>[3];
            for (int i = 0; i < source.Length; i++)
                source[i] = new BlockingCollection<int>(100);

            Parallel.For(0, source.Length * 100, (data) =>
            {
                int item = BlockingCollection<int>.TryAddToAny(source, data);
                if (item >= 0)
                    Console.WriteLine("added {0} to source data", data);
            });

            foreach (var array in source)
                array.CompleteAdding();

            // calculate the square
            var calculateFilter = new PipelineFilter<int, int>(source,
                (n) => n * n,
                "calculateFilter"
             );

            //Convert ints to strings
            var convertFilter = new PipelineFilter<int, string>
            (
                calculateFilter.m_outputData,
                (s) => String.Format("{0}", s),
                "convertFilter"
             );

            // Displays the results
            var displayFilter = new PipelineFilter<string, string>
            (
                convertFilter.m_outputData,
                (s) => Console.WriteLine("The final result is {0}", s),
                "displayFilter");

            // Start the pipeline
            try
            {
                Parallel.Invoke(
                             () => calculateFilter.Run(),
                             () => convertFilter.Run(),
                             () => displayFilter.Run()
                         );
            }
            catch (AggregateException aggregate)
            {
                foreach (var exception in aggregate.InnerExceptions)
                    Console.WriteLine(exception.Message + exception.StackTrace);
            }

            Console.ReadLine();
        }

        public static void Main(string[] args)
        {
            ParalellReduce.SumPrimeNumber_Reducer();

            ProcessBooksWithMapReduce.Run();

            WordsCounterDemo.Run();

            Demo.PrintSeparator();
            Console.WriteLine("parallel Reduce function implementation using Aggregate");

        }
    }
}



