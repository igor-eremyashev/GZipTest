using System;
using System.Threading.Tasks;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace GZipTest.Cli
{
    public abstract class ProcessCommandBase : ICommand
    {
        [CommandParameter(0, Description = "Path to file to compress.")]
        public string InputFile { get; init; }

        [CommandParameter(1, Description = "Path to result file.")]
        public string OutputFile { get; init; }

        [CommandOption("chunk_size", 'c', Description = "Chunk size in bytes.")]
        public int ChunkSize { get; init; } = 1024 * 1024;

        [CommandOption("chunk_pool_size", 'p', Description = "Chunk pool size.")]
        public int ChunkPoolSize { get; init; } = 10;

        [CommandOption("degree_of_parallelism", 'd', Description = "Number of simultaneous processing threads.")]
        public int DegreeOfParallelism { get; init; } = Environment.ProcessorCount;

        public abstract ValueTask ExecuteAsync(IConsole console);
    }
}