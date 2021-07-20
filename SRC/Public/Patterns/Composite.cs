/********************************************************************************
* Composite.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
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
    [SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix")]
    [SuppressMessage("Design", "CA1033:Interface methods should be callable by child types", Justification = "Derived types can access these methods via the Children property")]
    public abstract class Composite<TInterface> : Disposable, ICollection<TInterface>, IComposite<TInterface> where TInterface : class, IComposite<TInterface>
    {
        #region Private
        private readonly ConcurrentDictionary<TInterface, byte> FChildren = new();

        private int FCount; // kulon kell szamon tartani

        private TInterface? FParent;

        private TInterface Self 
        {
            get
            {
                TInterface? result = (this as TInterface);
                Assert(result is not null);
                return result!;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static MethodInfo GetCallerMethod() => (MethodInfo) new StackFrame(skipFrames: 2, fNeedFileInfo: false).GetMethod();

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
        /// <returns>Values returned by the <see cref="Children"/>.</returns>
        protected internal IReadOnlyCollection<TResult> Dispatch<TResult>(Func<TInterface, TResult> callback)
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
        protected Composite(TInterface? parent, int maxChildCount = int.MaxValue)
        {
            Ensure.Type<TInterface>.IsInterface();
            Ensure.Type<TInterface>.IsSupportedBy(this);

            parent?.Children.Add(Self);

            MaxChildCount = maxChildCount;
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
                // Kivesszuk magunkat a szulo gyerekei kozul (kiveve ha gyoker elemunk van, ott nincs szulo).
                //

                FParent?.Children.Remove(Self);

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
            FParent?.Children.Remove(Self);

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
        public int MaxChildCount { get; }

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
        public TInterface? Parent
        {
            get => FParent;

            [MethodImpl(MethodImplOptions.NoInlining)]
            set
            {
                if (GetCallerMethod().GetCustomAttribute<CanSetParentAttribute>() == null)
                    throw new InvalidOperationException(Resources.CANT_SET_PARENT);

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
      
        [CanSetParent]
        [MethodImpl(MethodImplOptions.NoInlining)]
        void ICollection<TInterface>.Add(TInterface child)
        {
            Ensure.Parameter.IsNotNull(child, nameof(child));
            CheckNotDisposed();

            if (child.Parent != null) 
                throw new ArgumentException(Resources.BELONGING_ITEM, nameof(child));

            if (InterlockedExtensions.IncrementIfLessThan(ref FCount, MaxChildCount) is null)
                throw new InvalidOperationException(string.Format(Resources.Culture, Resources.TOO_MANY_CHILDREN, MaxChildCount));

            bool succeeded = FChildren.TryAdd(child, 0);
            Assert(succeeded, "Child already contained");

            child.Parent = Self;
        }

        [CanSetParent]
        [MethodImpl(MethodImplOptions.NoInlining)]
        bool ICollection<TInterface>.Remove(TInterface child)
        {
            Ensure.Parameter.IsNotNull(child, nameof(child));
            CheckNotDisposed();

            if (child.Parent != Self) 
                return false;
 
            bool succeeded = FChildren.TryRemove(child, out _);
            Assert(succeeded, "Child already removed");

            Interlocked.Decrement(ref FCount);

            child.Parent = null;

            return true;
        }

        bool ICollection<TInterface>.Contains(TInterface child) 
        {
            Ensure.Parameter.IsNotNull(child, nameof(child));
            CheckNotDisposed();

            return child.Parent == Self;
        }
/*
        [CanSetParent]
        [MethodImpl(MethodImplOptions.NoInlining)]
*/
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