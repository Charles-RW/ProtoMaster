using BenchmarkDotNet.Attributes;
using ProtoMaster.Plugin.E01;
using System.IO;
using System;
using System.Collections.Generic;
using Microsoft.VSDiagnostics;

namespace ProtoMaster.Plugin.E01.Benchmarks
{
    [MemoryDiagnoser]
    public class ProtoDataParserBenchmark
    {
        private string _testDir;
        private ProtoDataParser _parser;

        [GlobalSetup]
        public void Setup()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "ProtoDataParserBenchmark_" + Guid.NewGuid());
            Directory.CreateDirectory(_testDir);
            
            // Create dummy data
            // Simulate some data to trigger parsing
            int fileCount = 2;
            int framesPerFile = 100;
            int dataSize = 1024;
            for (int i = 0; i < fileCount; i++)
            {
                string binPath = Path.Combine(_testDir, $"{ProtoDataParser.BIN_PREFIX}{i}");
                string lenPath = Path.Combine(_testDir, $"{ProtoDataParser.LEN_PREFIX}{i}");
                using (var binWriter = new BinaryWriter(File.Open(binPath, FileMode.Create)))
                using (var lenWriter = new StreamWriter(File.Open(lenPath, FileMode.Create)))
                {
                    for (int j = 0; j < framesPerFile; j++)
                    {
                        // Write LEN line
                        // Format: Timestamp, ?, DataID, DataLength
                        // Timestamp: yyyy-MM-dd-HH:mm:ss:fff
                        string timestamp = DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss:fff");
                        int dataId = 26; // Use a valid ID so it parses
                        lenWriter.WriteLine($"{timestamp}, 0, {dataId}, {dataSize}");
                        // Write BIN data
                        byte[] data = new byte[dataSize];
                        binWriter.Write(data);
                    }
                }
            }
        }

        [IterationSetup]
        public void IterationSetup()
        {
            _parser = new ProtoDataParser();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, true);
            }
        }

        [Benchmark]
        public object ParseDirectory()
        {
            return _parser.ParseDirectory(_testDir);
        }

        [Benchmark]
        public void ParseDirectoryLazy()
        {
            foreach (var item in _parser.ParseDirectoryLazy(_testDir))
            {
                // Consume but don't store
            }
        }
    }
}