using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace GZipTest.Benchmarks
{
    class Program
    {
        static async Task Main()
        {
            var poolSizes = new[] { 10, 100, 1000 };
            var chunkSizes = new[] { 1024 * 1024, 2 * 1024 * 1024 };
            var degreesOfParallelism = new[] { 1, 2, 4, 6, 8 };

            var initialFilePath = @"D:\Projects\GZipTest\GZipTest.Benchmarks\medium.txt";
            var resultFilePath = @"D:\Projects\GZipTest\GZipTest.Benchmarks\result.gzip";

            Console.WriteLine("Pool size - Chunk size - Degree of parallelism - runtime (ms)");

            foreach (int poolSize in poolSizes)
            foreach (int chunkSize in chunkSizes)
            foreach (int degreeOfParallelism in degreesOfParallelism)
            {
                var chunkPool = new ChunkPool(poolSize, chunkSize);
                var compressor = new Compressor(chunkPool, degreeOfParallelism);
                
                File.Delete(resultFilePath);

                var stopwatch = new Stopwatch();
                stopwatch.Start();
         
                await compressor.ProcessAsync(initialFilePath, resultFilePath);
                
                stopwatch.Stop();

                Console.WriteLine($"{poolSize} - {chunkSize} - {degreeOfParallelism}: {stopwatch.ElapsedMilliseconds}ms");
            }
        }
    }
}
