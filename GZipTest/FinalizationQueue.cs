using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GZipTest
{
    public class FinalizationQueue
    {
        private int _nextChunkNumber;
        private readonly SortedDictionary<int, Chunk> _pendingChunks;
        private readonly BlockingCollection<Chunk> _sortedChunks;

        public FinalizationQueue()
        {
            _nextChunkNumber = 0;
            _pendingChunks = new SortedDictionary<int, Chunk>();
            _sortedChunks = new BlockingCollection<Chunk>();
        }

        public void Add(Chunk chunk)
        {
            lock (_pendingChunks)
            {
                Console.WriteLine($"Chunk #{chunk.Sequence} ({chunk.GetHashCode()}) placed into finalization queue by thread {Thread.CurrentThread.ManagedThreadId}");

                _pendingChunks.Add(chunk.Sequence, chunk);

                while (true)
                {
                    Console.WriteLine($"Chunks ready for write: {string.Join(", ", _pendingChunks.Select(x => x.Key))}");

                    if (_pendingChunks.Count == 0)
                    {
                        break;
                    }

                    var firstChunkInQueue = _pendingChunks.First();

                    if (firstChunkInQueue.Key != _nextChunkNumber)
                    {
                        break;
                    }

                    _pendingChunks.Remove(firstChunkInQueue.Key);

                    _sortedChunks.Add(firstChunkInQueue.Value);

                    if (firstChunkInQueue.Value.IsEof)
                    {
                        _sortedChunks.CompleteAdding();
                    }

                    _nextChunkNumber++;
                }
            }
        }

        public IEnumerable<Chunk> GetConsumingEnumerable(CancellationToken cancellationToken)
        {
            return _sortedChunks.GetConsumingEnumerable(cancellationToken);
        }
    }
}