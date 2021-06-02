/********************************************************************************
* ExclusiveBlock.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Solti.Utils.Primitives.Threading
{
    using Patterns;
    using Properties;

    /// <summary>
    /// Describes the features that the <see cref="ExclusiveBlock"/> should support.
    /// </summary>
    [Flags]
    public enum ExclusiveBlockFeatures
    {
        /// <summary>
        /// Default features.
        /// </summary>
        None = 0,

        /// <summary>
        /// The <see cref="ExclusiveBlock.Enter()"/> method may be called recursively.
        /// </summary>
        SupportsRecursion = 1
    }

    /// <summary>
    /// Ensures operation exclusivity without lock.
    /// </summary>
    /// <remarks>This class is intended to signal the attempt to call non thread safe code parallelly.</remarks>
    public sealed class ExclusiveBlock : Disposable
    {
        private int FEntered;

        private readonly ThreadLocal<int>? FDepth;

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
            if (FDepth is null || --FDepth.Value is 0)
                Interlocked.Exchange(ref FEntered, 0);
        }

        /// <summary>
        /// Contains the disposal related logic.
        /// </summary>
        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
                FDepth?.Dispose();

            base.Dispose(disposeManaged);
        }

        /// <summary>
        /// Creates a new <see cref="ExclusiveBlock"/> instance.
        /// </summary>
        public ExclusiveBlock(ExclusiveBlockFeatures features = ExclusiveBlockFeatures.SupportsRecursion)
        {
            Features = features;

            if (Features.HasFlag(ExclusiveBlockFeatures.SupportsRecursion))
                FDepth = new ThreadLocal<int>(() => 0, trackAllValues: false);
        }

        /// <summary>
        /// Describes the features of this instance.
        /// </summary>
        public ExclusiveBlockFeatures Features { get; }

        /// <summary>
        /// Gets the scope for an exclusive operation.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)] // StackFrame jo helyre mutasson
        public IDisposable Enter() 
        {
            //
            // Ha ez a metodus nem lehet hivva rekurzivan akkor egyszeruen csak megnezzuk h korabban
            // mar volt e igenyelve a kizerolaoggas.
            //

            if (FDepth is null)
                TryEnter();

            //
            // Kulonben szalankent nyilvan kell tartsuk a melyseget (nyilvan a legelso hivo szal melysege
            // mehet csak 1 fole) es rekurzio eseten nem probalunk meg ismet kizarolagossagot igenyelni.
            //

            else
            {
                if (FDepth.Value is 0)
                    //
                    // Nem volt rekurzio (ebben a szalban) egyenlore
                    //

                    TryEnter();

                FDepth.Value++;
            }

            return new Scope(this);

            [MethodImpl(MethodImplOptions.NoInlining)]
            void TryEnter() 
            {
                //
                // Ha uj jatekosok vagyunk akkor ellenorizzuk h vki mas mar igenyelte e a
                // kizarolagossagot, ha igen akkor kivetel.
                //

                if (Interlocked.CompareExchange(ref FEntered, -1, 0) is not 0)
                {
                    var ex = new InvalidOperationException(Resources.NOT_EXCLUSIVE);
                    ex.Data["method"] = new StackFrame(skipFrames: 2).GetMethod();

                    throw ex;
                }
            }
        }
    }
}
