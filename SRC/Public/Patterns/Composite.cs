/********************************************************************************
* Composite.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using static System.Diagnostics.Debug;

namespace Solti.Utils.Primitives.Patterns
{
    using Properties;
    using Threading;

    /// <summary>
    /// Implements the <see cref="IComposite{TInterface}"/> interface.
    /// </summary>
    /// <typeparam name="TInterface">The interface on which we want to apply the composite pattern.</typeparam>
    public abstract class Composite<TInterface> : DisposableSupportsNotifyOnDispose, IComposite<TInterface> where TInterface : class, IDisposableEx, INotifyOnDispose
    {
        #region Private
        private readonly ConcurrentChildCollection FChildren;

        private static int FUsedTasks; // NEM globalis, leszarmazottankent ertelmezett

        private sealed class ConcurrentChildCollection : ICollection<TInterface>
        {
            private readonly ConcurrentLinkedList<TInterface> FUnderlyingList = new();

            private int FCount; // kulon kell szamon tartani

            public ConcurrentChildCollection(int maxSize) => MaxSize = maxSize;

            public int Count => FCount;

            public int MaxSize { get; }

            public bool IsReadOnly { get; }

            public void Add(TInterface item)
            {
                Ensure.Parameter.IsNotNull(item, nameof(item));

                //
                // Itt ne az FUnderlyingList.Count-ra vizsgaljunk mert az az ellenorzes pillanataban meg lehet h jo,
                // viszont mire az Add()-hez jutunk mar elromolhat.
                // 

                if (InterlockedExtensions.IncrementIfLessThan(ref FCount, MaxSize) is null)
                    throw new InvalidOperationException(string.Format(Resources.Culture, Resources.MAX_SIZE_REACHED, MaxSize));

                LinkedListNode<TInterface> node = new()
                {
                    Value = item
                };

                FUnderlyingList.Add(node);

                item.OnDispose += (_, _) =>
                {
                    FUnderlyingList.Remove(node);
                    Interlocked.Decrement(ref FCount);
                };
            }

            public bool Contains(TInterface item)
            {
                Ensure.Parameter.IsNotNull(item, nameof(item));

                return FUnderlyingList.Any(node => node.Value == item);
            }

            public IEnumerator<TInterface> GetEnumerator() => FUnderlyingList
                .Select(node => node.Value!)
                .GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public List<TInterface> ToList()
            {
                List<TInterface> lst = new(capacity: Count); // Count valtozhat a felsorolas alatt de kb jo

                foreach (TInterface child in this)
                {
                    lst.Add(child);
                }

                return lst;
            }

            public void Clear() =>
                //
                // Elemek csak ugy torolhetok ha azokon hivjuk a Dispose()-t
                //

                throw new NotSupportedException();

            public bool Remove(TInterface item) =>
                //
                // Elemek csak ugy torolhetok ha azokon hivjuk a Dispose()-t
                //

                throw new NotSupportedException();

            public void CopyTo(TInterface[] array, int arrayIndex) =>
                //
                // Nehez lehet biztonsaggal hivni mert az elemek szama felsorolas kozben is valtozhat.
                //

                throw new NotSupportedException();
        }
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

            ICollection<TInterface> children = FChildren.ToList(); // masolat

            TResult[] result = new TResult[children.Count];

            List<Task> boundTasks = new(capacity: MaxDegreeOfParallelism);

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
        protected Composite(IComposite<TInterface>? parent = null, int maxChildCount = int.MaxValue)
        {
            Ensure.Type<TInterface>.IsInterface();
            TInterface self = Ensure.Type<TInterface>.IsSupportedBy(this);

            FChildren = new ConcurrentChildCollection(maxChildCount);

            Parent = parent;
            Parent?.Children.Add(self);
        }

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
        /// Gets or sets the maximum number of concurrent tasks that the <see cref="Composite{TInterface}.Dispatch{TResult}(Func{TInterface, TResult})"/> method may use.
        /// </summary>
        [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "The value of this property may differ per descendants.")]
        public static int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;

        /// <summary>
        /// The parent of this entity. Can be null.
        /// </summary>
        public IComposite<TInterface>? Parent { get; private set; }

        /// <summary>
        /// The maximum number of children.
        /// </summary>
        public int MaxChildCount => FChildren.MaxSize;

        /// <summary>
        /// The children of this entity.
        /// </summary>
        public virtual ICollection<TInterface> Children
        {
            get
            {
                CheckNotDisposed();

                return FChildren;
            }
        }
        #endregion
    }
}