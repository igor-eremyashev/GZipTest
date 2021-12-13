using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace GZipTest
{
    public abstract class ChunkProcessor
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        private readonly ChunkPool _chunkPool;
        private readonly int _degreeOfParallelism;

        protected ChunkProcessor(ChunkPool chunkPool, int degreeOfParallelism)
        {
            _chunkPool = chunkPool;
            _degreeOfParallelism = degreeOfParallelism;
        }

        public async Task ProcessAsync(string initialFilePath, string processedFilePath)
        {
            var processingQueue = new BlockingCollection<Chunk>();
            var finalizationQueue = new FinalizationQueue();
            var onErrorCts = new CancellationTokenSource();

            var readTask = ReadChunksForProcessingAsync(initialFilePath, processingQueue, onErrorCts);

            var processingTasks = Enumerable.Range(0, _degreeOfParallelism)
                .Select(_ => { return Task.Run(() => { Process(processingQueue, finalizationQueue, onErrorCts); }); })
                .ToArray();

            var writeTask = SaveChunksAsync(processedFilePath, finalizationQueue, onErrorCts);

            var tasks = new List<Task>();

            tasks.Add(readTask);
            tasks.AddRange(processingTasks);
            tasks.Add(writeTask);

            await Task.WhenAll(tasks);
        }

        protected abstract Task ReadChunkAsync(Stream stream, Chunk chunk, CancellationToken cancellationToken);

        protected abstract void ProcessChunk(Chunk chunk);

        protected abstract Task WriteChunkAsync(Stream stream, Chunk chunk, CancellationToken cancellationToken);

        private async Task ReadChunksForProcessingAsync(
            string initialFilePath, 
            BlockingCollection<Chunk> processingQueue, 
            CancellationTokenSource cancellationTokenSource)
        {
            try
            {
                await using var stream = new FileStream(initialFilePath, FileMode.Open);

                var sequence = 0;

                while (true)
                {
                    var chunk = await _chunkPool.BorrowAsync(cancellationTokenSource.Token);

                    await ReadChunkAsync(stream, chunk, cancellationTokenSource.Token);
                    chunk.Sequence = sequence++;

                    Logger.Debug($"Chunk #{chunk.Sequence} ({chunk.GetHashCode()}) filled with data");

                    processingQueue.Add(chunk);

                    Logger.Debug($"Chunk #{chunk.Sequence} ({chunk.GetHashCode()}) placed into processing queue");

                    if (chunk.IsEof)
                    {
                        Logger.Debug("Initial file finished");
                        processingQueue.CompleteAdding();
                        return;
                    }
                }
            }
            catch
            {
                cancellationTokenSource.Cancel();
                throw;
            }
        }

        private void Process(
            BlockingCollection<Chunk> processingQueue, 
            FinalizationQueue finalizationQueue, 
            CancellationTokenSource cancellationTokenSource)
        {
            try
            {
                foreach (Chunk chunk in processingQueue.GetConsumingEnumerable(cancellationTokenSource.Token))
                {
                    Logger.Debug($"Chunk #{chunk.Sequence} ({chunk.GetHashCode()}) retrieved from processing queue by thread {Thread.CurrentThread.ManagedThreadId}");

                    if (!chunk.IsEof)
                    {
                        ProcessChunk(chunk);
                    }

                    Logger.Debug($"Chunk #{chunk.Sequence} ({chunk.GetHashCode()}) processed by thread {Thread.CurrentThread.ManagedThreadId}");

                    finalizationQueue.Add(chunk);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
                cancellationTokenSource.Cancel();
                throw;
            }
        }

        private async Task SaveChunksAsync(
            string processedFilePath, 
            FinalizationQueue finalizationQueue, 
            CancellationTokenSource cancellationTokenSource)
        {
            await using var stream = new FileStream(processedFilePath, FileMode.Create);

            try
            {
                foreach (var chunk in finalizationQueue.GetConsumingEnumerable(cancellationTokenSource.Token))
                {
                    if (chunk.IsEof)
                    {
                        _chunkPool.Return(chunk);
                        return;
                    }

                    await WriteChunkAsync(stream, chunk, cancellationTokenSource.Token);

                    Logger.Debug($"Chunk #{chunk.Sequence} ({chunk.GetHashCode()}) written to disk");

                    _chunkPool.Return(chunk);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
                cancellationTokenSource.Cancel();
                throw;
            }
            finally
            {
                stream.Flush(true);
                stream.Close();
            }
        }
    }
}