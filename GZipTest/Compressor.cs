using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace GZipTest
{
    public class Compressor : ChunkProcessor
    {
        public Compressor(ChunkPool chunkPool, int degreeOfParallelism) : base(chunkPool, degreeOfParallelism)
        {
        }

        protected override async Task ReadChunkAsync(Stream stream, Chunk chunk, CancellationToken cancellationToken)
        {
            var bytesRead = await stream.ReadAsync(chunk.UncompressedData, 0, chunk.Size, cancellationToken);

            if (bytesRead == 0)
            {
                chunk.IsEof = true;
            }

            chunk.UncompressedSize = bytesRead;
        }

        protected override void ProcessChunk(Chunk chunk)
        {
            using var memoryStream = new MemoryStream(chunk.CompressedData);
            using var zipStream = new GZipStream(memoryStream, CompressionMode.Compress);
            
            zipStream.Write(chunk.UncompressedData, 0, chunk.UncompressedSize);
            zipStream.Flush();

            chunk.CompressedSize = (int)memoryStream.Position;
        }

        protected override async Task WriteChunkAsync(Stream stream, Chunk chunk, CancellationToken cancellationToken)
        {
            await stream.WriteAsync(BitConverter.GetBytes(chunk.CompressedSize), cancellationToken);
            await stream.WriteAsync(chunk.CompressedData, 0, chunk.CompressedSize, cancellationToken);
        }
    }
}