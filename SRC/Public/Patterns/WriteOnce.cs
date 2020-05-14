/********************************************************************************
* WriteOnce.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Primitives
{
    using Properties;

    /// <summary>
    /// Represents a variable that can be set only once.
    /// </summary>
    public sealed class WriteOnce
    {
        private object? FValue;

        public WriteOnce(bool strict = true) => Strict = strict;

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
                if (!HasValue && Strict) throw new InvalidOperationException(); // TODO
                return FValue;
            }
            set
            {
                if (HasValue) throw new InvalidOperationException(Resources.VALUE_ALREADY_SET);
                FValue = value;
                HasValue = true;
            }
        }        
    }
}
