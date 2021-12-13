using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NLog;

namespace GZipTest
{
    public class FinalizationQueue
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

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
                Logger.Debug($"Chunk #{chunk.Sequence} ({chunk.GetHashCode()}) placed into finalization queue by thread {Thread.CurrentThread.ManagedThreadId}");

                _pendingChunks.Add(chunk.Sequence, chunk);

                while (true)
                {
                    Logger.Debug($"Chunks ready for write: {string.Join(", ", _pendingChunks.Select(x => x.Key))}");

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