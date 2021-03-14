/********************************************************************************
* StackFrame.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Reflection;
using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.Primitives.Perf
{
    using static Consts;

    [MemoryDiagnoser]
    public class StackFrame
    {
        [Params(0, 1, 2, 5)] // BenchmarkDotNet miatt legalabb ilyen hosszu a hivasi lanc.
        public int SkipFrames { get; set; }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void GetCaller()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                MethodBase _ = new System.Diagnostics.StackFrame(SkipFrames, needFileInfo: false).GetMethod();
            }
        }
    }
}
