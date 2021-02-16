/********************************************************************************
* Exclusive.cs                                                                  *
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
    public sealed class Exclusive : Disposable
    {
        private int FGlobalState;

        private readonly ThreadLocal<bool> FLocalState = new ThreadLocal<bool>(() => false, trackAllValues: false);

        private sealed class Scope : Disposable
        {
            private readonly Exclusive FOwner;

            public Scope(Exclusive owner) => FOwner = owner;

            protected override void Dispose(bool disposeManaged)
            {
                FOwner.Leave();
                base.Dispose(disposeManaged);
            }
        }

        private void Leave()
        {
            Interlocked.Exchange(ref FGlobalState, 0);
            FLocalState.Value = false;
        }

        /// <summary>
        /// Contains the disposal related logic.
        /// </summary>
        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
                FLocalState.Dispose();

            base.Dispose(disposeManaged);
        }

        /// <summary>
        /// Gets a scope for an exclusive operation.
        /// </summary>
        public IDisposable Enter() 
        {
            //
            // Ugyanaz a szal tobbszor is kerhet kizarolagossagot. Rekurzio eseten csak a legelso igenylest
            // kell kiszolgalni mert nyilvan az fog legutoljara veget erni
            //

            if (FLocalState.Value)
                return new Disposable();

            //
            // Ha vki mas is mar igenyelt kizarolagossagot akkor kivetel
            //

            if (Interlocked.CompareExchange(ref FGlobalState, 1, 0) != 0)
                throw new InvalidOperationException(Resources.NOT_EXCLUSIVE);

            FLocalState.Value = true;
            return new Scope(this);          
        }
    }
}
