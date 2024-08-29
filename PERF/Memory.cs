/********************************************************************************
* Memory.cs                                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.Primitives.Perf
{
    [MemoryDiagnoser]
    public class MurmurHash
    {
        [Params("", "a", "ab", "abcd", "abcdefgh", "abcdefghijklmnopqrstuvwz")]
        public string Input { get; set; } = null!;

        [Params(true, false)]
        public bool IgnoreCase { get; set; }

        [Benchmark(Baseline = true)]
        public int GetNativeHashCode() => string.GetHashCode(Input, IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

        [Benchmark]
        public int GetMurmurHashCode() => Input.AsSpan().GetHashCode(IgnoreCase);
    }

    [MemoryDiagnoser]
    public class IndexOfAnyExcept
    {
        const string TEST_STR = "0123456789+-.eE";

        public static IEnumerable<string> Inputs
        {
            get
            {
                yield return "";
                yield return new string(TEST_STR.Substring(0, 1));
                yield return new string(TEST_STR.Substring(0, 2));
                yield return new string(TEST_STR.Substring(0, 5));
                yield return TEST_STR + TEST_STR + TEST_STR;
            }
        }

        [ParamsSource(nameof(Inputs))]
        public string Input { get; set; } = null!;
/*
        [Benchmark(Baseline = true)]
        public int IndexOfAnyExceptNative() => System.MemoryExtensions.IndexOfAnyExcept(Input.AsSpan(), TEST_STR.AsSpan());

        [Benchmark]
        public int IndexOfAnyExceptWithoutContext() => MemoryExtensions.IndexOfAnyExcept(Input.AsSpan(), TEST_STR.AsSpan());
*/
        private const int OperationsPerInvoke = 10000;

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void IndexOfAnyExceptWithContext()
        {
            MemoryExtensions.ParsedSearchValues parsedSearchValues = default;

            _ = MemoryExtensions.IndexOfAnyExcept(Input.AsSpan(), TEST_STR.AsSpan(), ref parsedSearchValues);

            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                _ = MemoryExtensions.IndexOfAnyExcept(Input.AsSpan(), default, ref parsedSearchValues);
            }
        }
/*
        [Benchmark]
        public int IndexOfAnyExceptNotOptimized()
        {
            for (int i = 0; i < Input.Length; i++)
            {
                if (TEST_STR.IndexOf(Input[i]) < 0)
                {
                    return i;
                }
            }
            return -1;
        }
*/
    }
}
