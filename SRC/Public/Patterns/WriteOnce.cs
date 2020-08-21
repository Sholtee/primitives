/********************************************************************************
* WriteOnce.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Primitives.Patterns
{
    using Properties;

    /// <summary>
    /// Represents a variable that can be set only once.
    /// </summary>
    public sealed class WriteOnce
    {
        private object? FValue;

        /// <summary>
        /// Creates a new <see cref="WriteOnce"/> instance.
        /// </summary>
        public WriteOnce(bool strict = true) => Strict = strict;

        /// <summary>
        /// Creates a new <see cref="WriteOnce"/> instance.
        /// </summary>
        public WriteOnce(object? initialValue) : this(strict: false) => FValue = initialValue;

        /// <summary>
        /// Returns true if this instance is strict.
        /// </summary>
        public bool Strict { get; }

        /// <summary>
        /// Indicates whether the <see cref="Value"/> has already been set.
        /// </summary>
        public bool HasValue { get; private set; }

        /// <summary>
        /// The held value of this instance.
        /// </summary>
        public object? Value
        {
            get
            {
                if (!HasValue && Strict)
                    throw new InvalidOperationException(Resources.NO_VALUE);

                return FValue;
            }
            set
            {
                if (HasValue) 
                    throw new InvalidOperationException(Resources.VALUE_ALREADY_SET);

                FValue = value;
                HasValue = true;
            }
        }        
    }
}
