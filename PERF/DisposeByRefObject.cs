/********************************************************************************
* DisposeByRefObject.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using BenchmarkDotNet.Attributes;

namespace Solti.Utils.Primitives.Perf
{
    using static Consts;

    [MemoryDiagnoser]
    public class DisposeByRefObject
    {
        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void AddRef() 
        {
            var obj = new Patterns.DisposeByRefObject();

            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                obj.AddRef();
            }
        }
    }
}
