﻿using System.Threading.Tasks;
using CliFx;

namespace GZipTest
{
    static class Program
    {
        public static async Task<int> Main()
        {
            return await new CliApplicationBuilder()
                .AddCommandsFromThisAssembly()
                .Build()
                .RunAsync();
        }
    }
}
