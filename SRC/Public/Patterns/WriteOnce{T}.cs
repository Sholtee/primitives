/********************************************************************************
* WriteOnce{T}.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Threading;

namespace Solti.Utils.Primitives.Patterns
{
    using Properties;

    /// <summary>
    /// Represents a variable that can be set only once.
    /// </summary>
    public class WriteOnce<T> where T: class
    {
        private static readonly object NULL = new();

        private object? FValue;

        /// <summary>
        /// Creates a new <see cref="WriteOnce{T}"/> instance.
        /// </summary>
        public WriteOnce(bool strict = true) => Strict = strict;

        /// <summary>
        /// Returns true if this instance is strict.
        /// </summary>
        public bool Strict { get; }

        /// <summary>
        /// Indicates whether the <see cref="Value"/> has already been set.
        /// </summary>
        public bool HasValue => FValue is not null;

        /// <summary>
        /// The held value of this instance.
        /// </summary>
        public T? Value
        {
            get
            {
                object? value = FValue;

                //
                // NULL-t allitottunk be erteknek (elso feltetelnek szerepeljen h helyesen mukodjunk
                // typeof(T) == typeof(object) esetben is).
                //

                if (value == NULL)
                    return null;

                if (value == null && Strict)
                    throw new InvalidOperationException(Resources.NO_VALUE);

                return (T?) value;
            }
            set
            {
                if (Interlocked.CompareExchange(ref FValue, value ?? NULL, null) is not null)
                    throw new InvalidOperationException(Resources.VALUE_ALREADY_SET);
            }
        }        
    }
}
