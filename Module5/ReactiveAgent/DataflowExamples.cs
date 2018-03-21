using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ReactiveAgent.CS
{
    class DataflowBufferBlock
    {
        // Simple Producer Consumer using TPL Dataflow BufferBlock
        BufferBlock<int> buffer = new BufferBlock<int>();

        async Task Producer(IEnumerable<int> values)
        {
            foreach (var value in values)
                buffer.Post(value);
            buffer.Complete();
        }
        async Task Consumer(Action<int> process)
        {
            while (await buffer.OutputAvailableAsync())
                process(await buffer.ReceiveAsync());

        }
        public async Task Run()
        {
            IEnumerable<int> range = Enumerable.Range(0, 100);
            await Task.WhenAll(Producer(range), Consumer(n =>
                Console.WriteLine($"value {n}")));
        }
    }

    class DataflowTransformActionBlocks
    {
        public void Run()
        {
            //  Download image using TPL Dataflow TransformBlock
            var fetchImageFlag = new TransformBlock<string, (string, byte[])>(
                async urlImage =>
                {
                    using (var webClient = new WebClient())
                    {
                        byte[] data = await webClient.DownloadDataTaskAsync(urlImage);
                        return (urlImage, data);
                    }
                });

            List<string> urlFlags = new List<string>{
                "Italy#/media/File:Flag_of_Italy.svg",
                "Spain#/media/File:Flag_of_Spain.svg",
                "United_States#/media/File:Flag_of_the_United_States.svg"
                };

            foreach (var urlFlag in urlFlags)
                fetchImageFlag.Post($"https://en.wikipedia.org/wiki/{urlFlag}");


            //  Persist data using TPL Dataflow ActionBlock
            var saveData = new ActionBlock<(string, byte[])>(async data =>
            {
                (string urlImage, byte[] image) = data;
                string filePath = urlImage.Substring(urlImage.IndexOf("File:") + 5);
                await Agents.File.WriteAllBytesAsync(filePath, image);
            });

            fetchImageFlag.LinkTo(saveData);
        }
    }

    class MultipleProducersExample
    {

        // Asynchronous producer/consumer using TPL Dataflow
        BufferBlock<int> buffer = new BufferBlock<int>(
            new DataflowBlockOptions { BoundedCapacity = 10 });

        async Task Produce(IEnumerable<int> values)
        {
            foreach (var value in values)
                await buffer.SendAsync(value); ;
        }

        async Task MultipleProducers(params IEnumerable<int>[] producers)
        {
            await Task.WhenAll(
                    (from values in producers select Produce(values)).ToArray())
                .ContinueWith(_ => buffer.Complete());
        }

        async Task Consumer(Action<int> process)
        {
            while (await buffer.OutputAvailableAsync())
                process(await buffer.ReceiveAsync());
        }

        public async Task Run()
        {
            IEnumerable<int> range = Enumerable.Range(0, 100);

            await Task.WhenAll(MultipleProducers(range, range, range),
                Consumer(n => Console.WriteLine($"value {n} - ThreadId{Thread.CurrentThread.ManagedThreadId}")));
        }
    }


    //public class DataFlowBlocksExamples
    //{
    //    public ITargetBlock<string> ActionBlockExample(Action<byte[]> processData)
    //    {
    //        var downloader = new ActionBlock<string>(async url =>
    //        {
    //            using (var webClient = new WebClient())
    //            {
    //                byte[] imageData = await webClient.DownloadDataTaskAsync(url);
    //                processData(imageData);
    //            }
    //        }, new ExecutionDataflowBlockOptions
    //        {
    //            MaxDegreeOfParallelism = 5
    //        });
    //        return downloader;
    //    }
    //}
}
