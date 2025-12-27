using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VSDiagnostics;

namespace BenchmarkSuite1
{
    [MemoryDiagnoser]
    public class ReflectionBenchmark
    {
        private TestData _data;
        private static readonly Dictionary<Type, PropertyInfo[]> _propertyCache = new();

        [GlobalSetup]
        public void Setup()
        {
            _data = new TestData
            {
                Id = 1,
                Name = "Test",
                Value = 3.14,
                Description = "Description",
                Timestamp = DateTime.Now
            };
        }

        [Benchmark(Baseline = true)]
        public object StandardReflection()
        {
            var type = _data.GetType();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            object lastVal = null;
            foreach (var prop in properties)
            {
                lastVal = prop.GetValue(_data);
            }
            return lastVal;
        }

        [Benchmark]
        public object CachedReflection()
        {
            var type = _data.GetType();
            if (!_propertyCache.TryGetValue(type, out var properties))
            {
                properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                _propertyCache[type] = properties;
            }

            object lastVal = null;
            foreach (var prop in properties)
            {
                lastVal = prop.GetValue(_data);
            }
            return lastVal;
        }

        private class TestData
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public double Value { get; set; }
            public string Description { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
}