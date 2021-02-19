/********************************************************************************
* InterlockedExtensions.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Runtime.CompilerServices;

using static System.Threading.Interlocked;

namespace Solti.Utils.Primitives
{
    internal static class InterlockedExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int? IncrementIfGreaterThan(ref int location, int comparand)
        {
            int snapshot;
            do
            {
                snapshot = location;
                if (snapshot <= comparand) return null;
            }
            while (CompareExchange(ref location, snapshot + 1, snapshot) != snapshot);
            return snapshot;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int? IncrementIfLessThan(ref int location, int comparand)
        {
            int snapshot;
            do
            {
                snapshot = location;
                if (snapshot >= comparand) return null;
            }
            while (CompareExchange(ref location, snapshot + 1, snapshot) != snapshot);
            return snapshot;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int? DecrementIfGreaterThan(ref int location, int comparand)
        {
            int snapshot;
            do
            {
                snapshot = location;
                if (snapshot <= comparand) return null;
            }
            while (CompareExchange(ref location, snapshot - 1, snapshot) != snapshot);
            return snapshot;
        }
    }
}
