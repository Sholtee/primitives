/********************************************************************************
* ExclusiveBlock.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Threading;

namespace Solti.Utils.Primitives.Threading
{
    using Patterns;
    using Properties;

    /// <summary>
    /// Ensures operation exclusivity without lock.
    /// </summary>
    /// <remarks>This class is intended to signal the attempt to call non thread safe code parallelly.</remarks>
    public sealed class ExclusiveBlock : Disposable
    {
        private int FEntered;

        private readonly ThreadLocal<int> FDepth = new ThreadLocal<int>(() => 0, trackAllValues: false);

        private sealed class Scope : Disposable
        {
            private readonly ExclusiveBlock FOwner;

            public Scope(ExclusiveBlock owner) => FOwner = owner;

            protected override void Dispose(bool disposeManaged)
            {
                FOwner.Leave();
                base.Dispose(disposeManaged);
            }
        }

        private void Leave()
        {
            if (--FDepth.Value == 0)
                Interlocked.Exchange(ref FEntered, 0);
        }

        /// <summary>
        /// Contains the disposal related logic.
        /// </summary>
        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
                FDepth.Dispose();

            base.Dispose(disposeManaged);
        }

        /// <summary>
        /// Gets a scope for an exclusive operation.
        /// </summary>
        public IDisposable Enter() 
        {
            if (FDepth.Value == 0)
            {
                //
                // Ha uj jatekosok vagyunk akkor ellenorizzuk h vki mas mar igenyelte e a
                // kizarolagossagot, ha igen akkor kivetel.
                //

                if (Interlocked.CompareExchange(ref FEntered, 1, 0) != 0)
                    throw new InvalidOperationException(Resources.NOT_EXCLUSIVE);
            }

            //
            // Ugyanaz a szal tobbszor is igenyelheti a kizarolagossagot, akkor a melyseget noveljuk.
            //

            FDepth.Value++;
            return new Scope(this);          
        }
    }
}
