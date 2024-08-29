/********************************************************************************
* MemoryExtensions.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Runtime.CompilerServices;

using static System.Diagnostics.Debug;
using static System.Runtime.CompilerServices.Unsafe;
using static System.Runtime.InteropServices.MemoryMarshal;

namespace Solti.Utils.Primitives
{
    using CharEntry = (char Char, int Next);

    /// <summary>
    /// Defines some extensions over the <see cref="ReadOnlySpan{T}"/> type
    /// </summary>
    public static class MemoryExtensions
    {
        /// <summary>
        /// Context used to speed up the <see cref="IndexOfAnyExcept(ReadOnlySpan{char}, ReadOnlySpan{char}, ref ParsedSearchValues)"/> method.
        /// </summary>
        public readonly ref struct ParsedSearchValues
        {
            internal ParsedSearchValues(int len)
            {
                //
                // DO NOT merge these two arrays as it significantly degrades the performance
                //

                Entries = new CharEntry[len];
                Buckets = new int[len];
            }

            internal readonly Span<CharEntry> Entries;

            internal readonly Span<int> Buckets;
        }

        /// <summary>
        /// Returns the index of the first character that is not in the <paramref name="searchValues"/>.
        /// </summary>
        /// <remarks>If <paramref name="searchValues"/> is not null this method assigns the <paramref name="parsedSearchValues"/> which can be used to speed up subsequent calls (against the same search values)</remarks>
        public static int IndexOfAnyExcept(this ReadOnlySpan<char> span, ReadOnlySpan<char> searchValues, ref ParsedSearchValues parsedSearchValues)
        {
            /*
            for (int i = 0; i < span.Length; i++)
            {
                if (searchValues.IndexOf(span[i]) < 0)
                {
                    return i;
                }
            }

            return -1;
            */

            if (searchValues != default)
            {
                parsedSearchValues = new ParsedSearchValues
                (
                    RoundUpToNextPowerOfTwo(searchValues.Length)
                );

                for (int i = 0; i < searchValues.Length; i++)
                {
                    char actual = searchValues[i];  // compiler will eliminate boundary checks
                    ref int bucket = ref parsedSearchValues.Buckets[(actual | (actual << 16)) & (parsedSearchValues.Buckets.Length - 1)];

                    ref CharEntry entry = ref parsedSearchValues.Entries[i];
                    entry.Char = actual;
                    entry.Next = bucket - 1;

                    bucket = i + 1;
                }
            }

            for (int i = 0; i < span.Length; i++)
            {
                char actual = span[i];  // compiler will eliminate boundary checks
                for (int j = parsedSearchValues.Buckets[(actual | (actual << 16)) & (parsedSearchValues.Buckets.Length - 1)] - 1; (uint) j < parsedSearchValues.Entries.Length;)
                {
                    CharEntry entry = parsedSearchValues.Entries[j];

                    if (entry.Char == actual)
                        goto nextChar; 

                    j = entry.Next;
                }
                return i;
                nextChar:;
            }

            return -1;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static int RoundUpToNextPowerOfTwo(int val)
            {
                val--;
                val |= val >> 1;
                val |= val >> 2;
                val |= val >> 4;
                val |= val >> 8;
                val |= val >> 16;
                return val + 1;
            }
        }

        /// <summary>
        /// Returns the index of the first character that is not in the <paramref name="searchValues"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfAnyExcept(this ReadOnlySpan<char> span, ReadOnlySpan<char> searchValues)
        {
            ParsedSearchValues parsedSearchValues = default;
            return span.IndexOfAnyExcept(searchValues, ref parsedSearchValues);
        }

        /// <summary>
        /// Hashes the given character span using MURMUR hash
        /// </summary>
        public static int GetHashCode(this ReadOnlySpan<char> self, bool ignoreCase, int seed = 1986)
        {
            Span<char> buffer = ignoreCase && self.Length >= 4 ? stackalloc char[4] : default;

            //
            // https://github.com/bryc/code/blob/master/jshash/hashes/murmurhash3.js
            //

            unchecked
            {
                uint h = (uint) seed, k; 

                int i = 0;
                for (int b = self.Length & -4; i < b; i += 4)
                {
                    ref char spanRef = ref GetReference(ignoreCase ? BlockToUpper(self.Slice(i, 4), buffer) : self.Slice(i, 4));  // to avoid boundary checks

                    k = (uint) (Add(ref spanRef, 3) << 24 | Add(ref spanRef, 2) << 16 | Add(ref spanRef, 1) << 8 | Add(ref spanRef, 0));
                    k *= 3432918353; k = k << 15 | k >> 17;
                    h ^= k * 461845907; h = h << 13 | h >> 19;
                    h = h * 5 + 3864292196;
                }

                int m = self.Length & 3;
                if (m > 0)
                {
                    ref char spanRef = ref GetReference(self);
                    k = 0;
                    switch (m)
                    {
                        case 3:
                            k ^= (uint) (ignoreCase ? CharToUpper(Add(ref spanRef, i + 2)) : Add(ref spanRef, i + 2)) << 16;
                            goto case 2;
                        case 2:
                            k ^= (uint) (ignoreCase ? CharToUpper(Add(ref spanRef, i + 1)) : Add(ref spanRef, i + 1)) << 8;
                            goto case 1;
                        case 1:
                            k ^= (uint) (ignoreCase ? CharToUpper(Add(ref spanRef, i)) : Add(ref spanRef, i));
                            k *= 3432918353; k = k << 15 | k >> 17;
                            h ^= k * 461845907;
                            break;
                    }
                }

                h ^= (uint) self.Length;

                h ^= h >> 16; h *= 2246822507;
                h ^= h >> 13; h *= 3266489909;
                h ^= h >> 16;

                return (int) h;
            }

            //
            // https://github.com/dotnet/runtime/blob/ecc8cb5bc0411e0fb0549230f70dfe8ab302c65c/src/libraries/System.Private.CoreLib/src/System/Text/Unicode/Utf16Utility.cs#L98
            //

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static ReadOnlySpan<char> BlockToUpper(ReadOnlySpan<char> chars, Span<char> buffer)
            {
                Assert(chars.Length is 4, "Invald block size");
                Assert(buffer.Length is 4, "Invalid buffer size");

                ulong l = As<char, ulong>(ref GetReference(chars));

                if ((l & ~0x007F_007F_007F_007Ful) is 0)
                {
                    //
                    // All the 4 chars are ASCII
                    //

                    ulong
                        lowerIndicator = l + 0x0080_0080_0080_0080ul - 0x0061_0061_0061_0061ul,              
                        upperIndicator = l + 0x0080_0080_0080_0080ul - 0x007B_007B_007B_007Bul,         
                        combinedIndicator = lowerIndicator ^ upperIndicator,
                        mask = (combinedIndicator & 0x0080_0080_0080_0080ul) >> 2;

                    As<char, ulong>(ref GetReference(buffer)) = l ^ mask;
                }
                else
                {
                    //
                    // Slow like hell
                    //

                    chars.ToUpperInvariant(buffer);
                }
                return buffer;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static char CharToUpper(char chr)
            {
                if ((chr & ~0x007Fu) is 0)
                {
                    uint
                        lowerIndicator = chr + 0x0080u - 0x0061u,
                        upperIndicator = chr + 0x0080u - 0x007Bu,
                        combinedIndicator = lowerIndicator ^ upperIndicator,
                        mask = (combinedIndicator & 0x0080u) >> 2;

                    return (char) (chr ^ mask);
                }

                //
                // Slow...
                //

                return char.ToUpperInvariant(chr);
            }
        }
    }
}
