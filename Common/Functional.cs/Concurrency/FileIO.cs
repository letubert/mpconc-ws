using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Functional.IO
{
    public static class FileAsync
    {
        private const int BUFFER_SIZE = 0x1000;

        public static FileStream OpenRead(string path) => new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BUFFER_SIZE, true);

        public static FileStream OpenWrite(string path) => new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, BUFFER_SIZE, true);

        private static async Task CopyStream(Stream input, Stream output, IProgress<int> progress)
        {
            byte[][] buffers = new byte[2][] { new byte[BUFFER_SIZE], new byte[BUFFER_SIZE] };
            int filledBufferNum = 0;
            int readBytes = await input.ReadAsync(buffers[filledBufferNum], 0, buffers[filledBufferNum].Length);

            while (readBytes > 0)
            {
                progress.Report(readBytes);
                Task writeTask = output.WriteAsync(buffers[filledBufferNum], 0, readBytes);
                filledBufferNum ^= 1;
                Task<int> readTask = input.ReadAsync(buffers[filledBufferNum], 0, buffers[filledBufferNum].Length);
                await Task.WhenAll(readTask, writeTask);
                readBytes = readTask.Result;
            }
        }

        public static async Task WriteAllBytesAsync(string path, byte[] bytes)
        {
            using (FileStream stream = OpenWrite(path))
            {
                await stream.WriteAsync(bytes, 0, bytes.Length)
                    .ContinueWith(task =>
                    {
                        var e = task.Exception;
                        stream.Dispose();
                        if (e != null) throw e;
                    }, TaskContinuationOptions.ExecuteSynchronously);
            }
        }

        public static async Task<byte[]> ReadAllBytesAsync(this Stream stream)
        {
            using (var destStream = new MemoryStream(stream.CanSeek ? (int)stream.Length : 0))
            {
                return await stream.CopyToAsync(destStream).ContinueWith(task =>
                {
                    var bytes = destStream.ToArray();
                    destStream.Dispose();
                    return bytes;
                });
            }
        }

        public static async Task WriteAllTextAsync(string path, string contents) =>
            await Task.Run(() => Encoding.UTF8.GetBytes(contents))
                .ContinueWith(async task => await WriteAllBytesAsync(path, task.Result));
    }
}