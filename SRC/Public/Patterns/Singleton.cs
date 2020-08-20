/********************************************************************************
* Singleton.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Solti.Utils.Primitives.Patterns
{
    using Properties;

    /// <summary>
    /// Implements the Singleton design pattern.
    /// </summary>
    public abstract class Singleton<TConcrete> where TConcrete: Singleton<TConcrete>
    {
        /// <summary>
        /// Creates a new <see cref="Singleton{TConcrete}"/> instance.
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        protected Singleton()
        {
            if (Instance != null)
                throw new InvalidOperationException(Resources.INSTANCE_ALREADY_CREATED);

            Instance = (TConcrete) this;
        }

        /// <summary>
        /// 
        /// </summary>
        [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "It is intentional to let each descendants have their own instance")]
        public static TConcrete? Instance { get; private set; }
    }
}
