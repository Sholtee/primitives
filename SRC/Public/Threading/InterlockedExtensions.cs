/********************************************************************************
* InterlockedExtensions.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Runtime.CompilerServices;

using static System.Threading.Interlocked;

namespace Solti.Utils.Primitives.Threading
{
    /// <summary>
    /// Defines some additions to the <see cref="System.Threading.Interlocked"/> class
    /// </summary>
    public static class InterlockedExtensions
    {
        /// <summary>
        /// Increments the first value if it is greater than the second one.
        /// </summary>
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

        /// <summary>
        /// Increments the first value if it is less than the second one.
        /// </summary>
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

        /// <summary>
        /// Decrements the first value if it is greater than the second one.
        /// </summary>
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

        /// <summary>
        /// Bitwise "ors" two integers and replaces the first value with the result.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Or(ref int location, int value)
        {
            int snapshot;
            do
            {
                snapshot = location;
            } while (CompareExchange(ref location, snapshot | value, snapshot) != snapshot);
            return snapshot;
        }

        /// <summary>
        /// Compares two integers and replaces the first value with the second one if the second value is bigger.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Max(ref int location, int comparand)
        {
            int snapshot;
            do
            {
                snapshot = location;
            } while (snapshot < comparand && CompareExchange(ref location, comparand, snapshot) != snapshot);
            return snapshot;
        }
    }
}
