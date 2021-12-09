using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GZipTest
{
    public class ChunkPool
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly ConcurrentBag<Chunk> _chunks;

        public ChunkPool(int chunkCount, int chunkSize)
        {
            _semaphore = new SemaphoreSlim(chunkCount);

            _chunks = new ConcurrentBag<Chunk>(Enumerable
                .Range(0, chunkCount)
                .Select(x => new Chunk(chunkSize))
                .ToList());
        }

        public async Task<Chunk> BorrowAsync(CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);

            if (_chunks.TryTake(out var result))
            {
                Console.WriteLine("Chunk borrowed");

                return result;
            }

            throw new InvalidOperationException();
        }

        public void Return(Chunk chunk)
        {
            Console.WriteLine($"Chunk #{chunk.Sequence} returned");

            chunk.Reset();

            _chunks.Add(chunk);
            
            _semaphore.Release();
        }
    }
}