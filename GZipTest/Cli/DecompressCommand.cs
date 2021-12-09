using System.Threading.Tasks;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace GZipTest.Cli
{
    [Command("decompress")]
    public class DecompressCommand : ProcessCommandBase
    {
        public override async ValueTask ExecuteAsync(IConsole console)
        {
            var chunkPool = new ChunkPool(ChunkPoolSize, ChunkSize);
            var decompressor = new Decompressor(chunkPool, DegreeOfParallelism);

            await decompressor.ProcessAsync(InputFile, OutputFile);
        }
    }
}