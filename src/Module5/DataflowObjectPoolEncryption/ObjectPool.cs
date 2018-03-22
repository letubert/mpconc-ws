using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace DataflowObjectPoolEncryption
{
    public class Chunk
    {
        public Chunk(int chunkSize)
        {
            Bytes = new byte[chunkSize];
        }

        public Chunk(byte[] bytes)
        {
            Bytes = bytes;
            Length = bytes.Length;
        }

        public byte[] Bytes { get; }
        public int Length { get; set; }
    }

    public class ObjectPoolAsync<T>
    {
        private readonly BufferBlock<T> buffer;
        private readonly Func<T> factory;
        private readonly int msecTimeout;
        private int currentSize;

        public ObjectPoolAsync(int initialCount, Func<T> factory, CancellationToken cts, int msecTimeout = 0)
        {
            this.msecTimeout = msecTimeout;

            buffer = new BufferBlock<T>(
                new DataflowBlockOptions { CancellationToken = cts }
             );
            this.factory = () => {
                Interlocked.Increment(ref currentSize);
                return factory();
            };
            for (int i = 0; i < initialCount; i++)
                buffer.Post(this.factory());
        }

        public int Size => currentSize;

        // TODO : 5.1
        // Complete the Send method that push an item to the local DataFlow block
        public Task<bool> Send(T item) =>
                Task.FromResult(false); // <<= replace this line of code with your implementation

        // (2)
        // Complete the GetAsync method that retrieves asynchronously
        // the recycled data from the ObjectPool
        public Task<T> GetAsync(int timeout = 0)
        {
            return Task.FromResult<T>(default(T)); // <<= replace this line of code with your implementation
        }

    }
}
