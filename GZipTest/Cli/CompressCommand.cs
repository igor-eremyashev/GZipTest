using System.Threading.Tasks;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace GZipTest.Cli
{
    [Command("compress")]
    public class CompressCommand : ProcessCommandBase
    {
        public override async ValueTask ExecuteAsync(IConsole console)
        {
            var chunkPool = new ChunkPool(ChunkPoolSize, ChunkSize);
            var compressor = new Compressor(chunkPool, DegreeOfParallelism);

            await compressor.ProcessAsync(InputFile, OutputFile);
        }
    }
}