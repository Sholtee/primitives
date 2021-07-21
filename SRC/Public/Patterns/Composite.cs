/********************************************************************************
* Composite.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using static System.Diagnostics.Debug;

namespace Solti.Utils.Primitives.Patterns
{
    using Properties;

    /// <summary>
    /// Implements the <see cref="IComposite{TInterface}"/> interface.
    /// </summary>
    /// <typeparam name="TInterface">The interface on which we want to apply the composite pattern.</typeparam>
    [SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix")]
    [SuppressMessage("Design", "CA1033:Interface methods should be callable by child types", Justification = "Derived types can access these methods via the Children property")]
    public abstract class Composite<TInterface> : Disposable, ICollection<TInterface>, IComposite<TInterface> where TInterface : class, IDisposableEx
    {
        #region Private
        private readonly ConcurrentDictionary<TInterface, byte> FChildren = new();

        private int FCount; // kulon kell szamon tartani

        private IComposite<TInterface>? FParent;

        private static int FUsedTasks; // NEM globalis, leszarmazottankent ertelmezett
        #endregion

        #region Protected
        //
        // Feldolgoz(elem)
        //   eredmeny[elem.gyerekek.length]
        //   feldolgozok[]
        //   For i := 0 to elem.gyerekek.length - 1
        //     HA van szabad feldolgzo
        //       feldolgozok.push(() => eredmeny[i] = Feldolgoz(elem.gyerekek[i]))
        //     KULONBEN
        //       eredmeny[i] = Feldolgoz(elem.gyerekek[i])
        //   HA feldolgozok.length > 0
        //      Megvar(feldolgozok)
        //   RETURN eredmeny
        //

        /// <summary>
        /// Executes the <paramref name="callback"/> against all the <see cref="Children"/>.
        /// </summary>
        /// <returns>Values returned by the <paramref name="callback"/> invocations. The position of the returned values match the position of their counterpart child element in the <see cref="Children"/> list.</returns>
        protected internal ICollection<TResult> Dispatch<TResult>(Func<TInterface, TResult> callback)
        {
            Ensure.Parameter.IsNotNull(callback, nameof(callback));

            //
            // Mivel itt a lista egy korabbi allapotaval dolgozunk ezert az iteracio alatt hozzaadott gyermekeken
            // nem, mig eltavolitott gyermekeken meg lesz hivva a cel metodus.
            //

            ICollection<TInterface> children = FChildren.Keys; // masolat

            TResult[] result = new TResult[children.Count];

            List<Task> boundTasks = new();

            //
            // Cel metodus hivasa rekurzivan az osszes gyerekre.
            //

            children.ForEach((child, itemIndex) =>
            {
                //
                // Ha van szabad Task akkor az elem es gyermekeinek feldolgozasat elinditjuk azon
                //

                int? taskIndex = InterlockedExtensions.IncrementIfLessThan(ref FUsedTasks, MaxDegreeOfParallelism);

                if (taskIndex is not null)
                    boundTasks.Add(Task.Run(() =>
                    {
                        WriteLine($"{nameof(Dispatch)}(): traversing parallelly ({taskIndex})");
                        try
                        {
                            result[itemIndex] = callback(child);
                        }
                        finally 
                        {
                            Interlocked.Decrement(ref FUsedTasks);
                        }
                    }));

                //
                // Kulonben helyben dolgozzuk fel
                //

                else
                    result[itemIndex] = callback(child);
            });

            if (boundTasks.Any())
                Task.WaitAll(boundTasks.ToArray()); 

            return result;
        }

        /// <summary>
        /// Executes the <paramref name="callback"/> against all the <see cref="Children"/>.
        /// </summary>
        protected internal void Dispatch(Action<TInterface> callback)
        {
            Ensure.Parameter.IsNotNull(callback, nameof(callback));
          
            Dispatch<object?>(i => 
            {
                callback(i);
                return null;
            });
        }

        /// <summary>
        /// Creates a new <see cref="Composite{TInterface}"/> instance.
        /// </summary>
        protected Composite() => Ensure.Type<TInterface>.IsInterface();

        /// <summary>
        /// Disposal logic related to this class.
        /// </summary>
        /// <param name="disposeManaged">Check out the <see cref="Disposable"/> class.</param>
        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                //
                // Torol munket a Parent.Children elemek kozul is.
                //

                Parent = null;

                //
                // Dispose() hivasa az osszes gyermeken.
                //

                Dispatch(i => i.Dispose());

                Assert(!FChildren.Any());
            }

            base.Dispose(disposeManaged);
        }

        /// <summary>
        /// Disposal logic related to this class.
        /// </summary>
        protected async override ValueTask AsyncDispose()
        {
            Parent = null;

            await Task.WhenAll
            (
                Dispatch(i => i.DisposeAsync()).Select(t => t.AsTask())
            );

            Assert(!FChildren.Any());

            //
            // Ne hivjuk a "base"-t mert azt a Dispose()-t hivna
            //
        }
        #endregion

        #region Public
        /// <summary>
        /// Returns the maximum child count that can be stored by this instance.
        /// </summary>
        public int MaxChildCount { get; init; } = int.MaxValue;

        /// <summary>
        /// Gets or sets the maximum number of concurrent tasks that the <see cref="Composite{TInterface}.Dispatch{TResult}(Func{TInterface, TResult})"/> method may use.
        /// </summary>
        [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "The value of this property may differ per descendants.")]
        public static int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
        #endregion

        #region IComposite
        /// <summary>
        /// The parent of this entity. Can be null.
        /// </summary>
        public IComposite<TInterface>? Parent
        {
            get => FParent;
            set
            {
                if (value == FParent)
                    return;

                //
                // Ahhoz h gyermekkent szerepelhessunk, nekunk is implementalni kell TInterface-t
                //

                TInterface self = Ensure.Type<TInterface>.IsSupportedBy(this);

                FParent?.Children.Remove(self);
                value?.Children.Add(self);

                FParent = value;
            }
        }

        /// <summary>
        /// The children of this entity.
        /// </summary>
        public virtual ICollection<TInterface> Children
        {
            get
            {
                CheckNotDisposed();

                return this;
            }
        }
        #endregion

        #region ICollection
        //
        // Csak a lenti tagoknak kell szalbiztosnak lenniuk.
        //

        int ICollection<TInterface>.Count 
        {
            get 
            {
                CheckNotDisposed();

                return FCount;
            }
        }

        bool ICollection<TInterface>.IsReadOnly => false;
      
        void ICollection<TInterface>.Add(TInterface child)
        {
            Ensure.Parameter.IsNotNull(child, nameof(child));
            CheckNotDisposed();

            //
            // lock() arra a perverz esetre ha ugyanazt a gyereket parhuzamosan tobbszor is
            // hozza akarnank adni.
            //

            lock (child)
            {
                //
                // Itt ne az FChildren.Count-ra vizsgaljunk mert az ellenorzes pillanataban az ertek meg lehet h jo,
                // viszont mire a TryAdd()-hez jutunk mar lehet elromlik
                // 

                if (InterlockedExtensions.IncrementIfLessThan(ref FCount, MaxChildCount) is null)
                    throw new InvalidOperationException(string.Format(Resources.Culture, Resources.MAX_SIZE_REACHED, MaxChildCount));

                //
                // TryAdd() nem tudom min lock-ol de meg ha a kulcson akkor sincs gond mert ugyanabban a szalban lesz
                // mint ahol a fenti lock volt hivva.
                //

                if (!FChildren.TryAdd(child, 0))
                {
                    Interlocked.Decrement(ref FCount);
                    throw new InvalidOperationException(Resources.ITEM_ALREADY_ADDED);
                }
            }
        }

        bool ICollection<TInterface>.Remove(TInterface child)
        {
            Ensure.Parameter.IsNotNull(child, nameof(child));
            CheckNotDisposed();

            lock (child)
                if (!FChildren.TryRemove(child, out _))
                    return false;

            Interlocked.Decrement(ref FCount);
            return true;
        }

        bool ICollection<TInterface>.Contains(TInterface child) 
        {
            Ensure.Parameter.IsNotNull(child, nameof(child));
            CheckNotDisposed();

            return FChildren.ContainsKey(child);
        }

        void ICollection<TInterface>.Clear() => throw new NotImplementedException();

        void ICollection<TInterface>.CopyTo(TInterface[] array, int arrayIndex)
        {
            Ensure.Parameter.IsNotNull(array, nameof(array));
            CheckNotDisposed();

            FChildren.Keys.CopyTo(array, arrayIndex);
        }

        IEnumerator<TInterface> IEnumerable<TInterface>.GetEnumerator()
        {
            CheckNotDisposed();

            return FChildren.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => Children.GetEnumerator();
        #endregion
    }
}