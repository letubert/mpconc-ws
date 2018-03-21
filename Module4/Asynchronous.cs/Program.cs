using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncBlobCloud.Asynchronous;

namespace ConsoleApplication1
{
    class Program
    {
        private static void DowloadFromCloudAsync()
        {

            var photoViewerPath = @"..\..\..\..\Common\PhotoViewer\App\PhotoViewer.exe";
            var tempImageFolder = @"..\..\..\..\Common\PhotoViewer\App\TempImageFolder";

            var currentDir = Environment.CurrentDirectory;
            var photoViewerPathProc = System.IO.Path.Combine(currentDir, photoViewerPath);

            if (File.Exists(photoViewerPathProc))
            {
                if (!Directory.Exists(tempImageFolder)) Directory.CreateDirectory(tempImageFolder);
                DirectoryInfo di = new DirectoryInfo(tempImageFolder);

                foreach (FileInfo file in di.GetFiles())
                    file.Delete();

                Process proc = new Process();
                proc.StartInfo.FileName = photoViewerPathProc;
                proc.StartInfo.WorkingDirectory = Path.GetDirectoryName(photoViewerPathProc);
                proc.StartInfo.Arguments = tempImageFolder;
                proc.Start();

                // TODO : 4.1
                // Async downloads
                var asyncBlobCloud = new AsyncBlobCloud.Asynchronous.AsyncBlobCloud();

                asyncBlobCloud.DownloadMedia(tempImageFolder);
                // asyncBlobCloud.DownloadMediaAsync(tempImageFolder).Wait();
                // asyncBlobCloud.DownloadInParallelAsync(tempImageFolder).Wait();
                // asyncBlobCloud.DownloadInParallelExecuteComplete(tempImageFolder).Wait();

                Console.WriteLine("Completed!!");
                Console.ReadLine();
            }
        }

        static void Main(string[] args)
        {
            // TODO : 4.1
            DowloadFromCloudAsync();

        }

        private static void CancelTask()
        {
             //  Cancellation Token callback
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;

            Task.Run(async () =>
            {
                var webClient = new WebClient();
                token.Register(() => webClient.CancelAsync());

                var data = await
                    webClient.DownloadDataTaskAsync("http://www.manning.com");
            }, token);

            tokenSource.Cancel();
        }
        void CooperativeCancellation()
        {
            //  Cooperative cancellation token
            CancellationTokenSource ctsOne = new CancellationTokenSource();
            CancellationTokenSource ctsTwo = new CancellationTokenSource();
            CancellationTokenSource ctsComposite = CancellationTokenSource.CreateLinkedTokenSource(ctsOne.Token, ctsTwo.Token);

            CancellationToken ctsCompositeToken = ctsComposite.Token;
            Task.Factory.StartNew(async () =>
            {
                var webClient = new WebClient();
                ctsCompositeToken.Register(() => webClient.CancelAsync());

                await webClient.DownloadDataTaskAsync("http://www.manning.com");
            }, ctsComposite.Token);
        }
    }
}
