using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace GZipTest
{
    public class Decompressor : ChunkProcessor
    {
        private readonly byte[] _sizeBuffer;

        public Decompressor(ChunkPool chunkPool, int degreeOfParallelism) : base(chunkPool, degreeOfParallelism)
        {
            _sizeBuffer = new byte[4];
        }

        protected override async Task ReadChunkAsync(Stream stream, Chunk chunk, CancellationToken cancellationToken)
        {
            var headerBytesRead = await stream.ReadAsync(_sizeBuffer, 0, _sizeBuffer.Length, cancellationToken);

            if (headerBytesRead == 0)
            {
                chunk.IsEof = true;
            }
            else
            {
                var compressedChunkSize = BitConverter.ToInt32(_sizeBuffer, 0);

                int chunkBytesRead = await stream.ReadAsync(chunk.CompressedData, 0, compressedChunkSize, cancellationToken);

                if (chunkBytesRead != compressedChunkSize)
                {
                    throw new InvalidFileFormatException();
                }

                chunk.CompressedSize = chunkBytesRead;
            }
        }

        protected override void ProcessChunk(Chunk chunk)
        {
            using var memoryStream = new MemoryStream(chunk.CompressedData, 0, chunk.CompressedSize);
            using var zipStream = new GZipStream(memoryStream, CompressionMode.Decompress);

            chunk.UncompressedSize = zipStream.Read(chunk.UncompressedData, 0, chunk.Size);
        }

        protected override async Task WriteChunkAsync(Stream stream, Chunk chunk, CancellationToken cancellationToken)
        {
            await stream.WriteAsync(chunk.UncompressedData, 0, chunk.UncompressedSize, cancellationToken);
        }
    }
}