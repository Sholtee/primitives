/********************************************************************************
* Composite.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
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
    /// Implements the <see cref="IComposite{T}"/> interface.
    /// </summary>
    /// <typeparam name="TInterface">The interface on which we want to apply the composite pattern.</typeparam>
    /// <remarks>This is an internal class so it may change from version to version. Don't use it!</remarks>
    [SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix")]
    [SuppressMessage("Design", "CA1033:Interface methods should be callable by child types", Justification = "Derived types can access these methods via the Children property")]
    public abstract class Composite<TInterface> : Disposable, ICollection<TInterface>, IComposite<TInterface> where TInterface : class, IComposite<TInterface>
    {
        #region Private
        private readonly HashSet<TInterface> FChildren = new HashSet<TInterface>();

        private readonly ReaderWriterLockSlim FLock = new ReaderWriterLockSlim();

        private readonly IReadOnlyDictionary<MethodInfo, MethodInfo> FInterfaceMapping;

        private TInterface? FParent;

        private TInterface Self => (this as TInterface)!;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static MethodInfo GetCallerMethod() => (MethodInfo) new StackFrame(skipFrames: 2, fNeedFileInfo: false).GetMethod();

        private static MethodInfo GetMethod(Expression<Action<TInterface>> expr) => ((MethodCallExpression) expr.Body).Method;

        private IReadOnlyDictionary<MethodInfo, MethodInfo> GetInterfaceMapping() 
        {
            return GetInterfaceMappingInternal(typeof(TInterface))
                //
                // Tekintsuk a kovetkezo esetet: IA: IDisposable, IB: IDisposable, IC: IA, IB -> Distinct()
                //

                .Distinct()
                .ToDictionary(kvp => kvp.TargetMethod, kvp => kvp.InterfaceMethod);
 
            IEnumerable<(MethodInfo TargetMethod, MethodInfo InterfaceMethod)> GetInterfaceMappingInternal(Type iface) 
            {
                InterfaceMapping mappings = GetType().GetInterfaceMap(iface);
                
                foreach (var mapping in mappings.TargetMethods.Select((tm, i) => (tm, mappings.InterfaceMethods[i])))
                {
                    yield return mapping;
                }

                foreach (var mapping in iface.GetInterfaces().SelectMany(GetInterfaceMappingInternal)) 
                {
                    yield return mapping;
                }
            }
        }

        private static Func<TInterface, object?[], object> ConvertToDelegate(MethodInfo method) 
        {
            //
            // Composite minta nem tamogatja a kimeno parametereket
            //

            if (method.GetParameters().Any(para => para.ParameterType.IsByRef))
                throw new NotSupportedException(string.Format(Resources.Culture, Resources.BYREF_PARAM_NOT_SUPPORTED, method.Name));

            return method.ToInstanceDelegate();
        }

        private IReadOnlyCollection<object> Dispatch(MethodInfo ifaceMethod, params object?[] args) 
        {
            //
            // 1) Ne generaljuk elore le az osszes delegate-et mert nem tudhatjuk h mely metodusok implementacioja
            //    fogja hivni a Dispatch()-et (nem biztos h az osszes).
            // 2) Generikus argumentumot tartalmazo metodushoz amugy sem tudnank legeneralni.
            //

            Func<TInterface, object?[], object> invoke = Cache.GetOrAdd(ifaceMethod, () => ConvertToDelegate(ifaceMethod));

            return Children
                //
                // Ez azert kell h:
                //   - Iteracio kozben is modosithato legyen a gyermek lista
                //   - Ne blokkoljuk a teljes listat hosszu ideig (az egyes "call" hivasok idoigenyesek lehetnek)
                //

                .ToArray()
                .Select(child => invoke(child, args))
                .ToArray();
        }
        #endregion

        #region Protected
        /// <summary>
        /// Forwards the arguments to all the child methods.
        /// </summary>
        /// <returns>Values returned by child methods.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        protected internal IReadOnlyCollection<object> Dispatch(Type[]? genericArgs, params object?[] args)
        {
            Ensure.Parameter.IsNotNull(args, nameof(args));

            //
            // GetCallerMEthod() mindig a generikus metodus definiciojat adja vissza.
            //

            if (!FInterfaceMapping.TryGetValue(GetCallerMethod(), out MethodInfo ifaceMethod))
                throw new InvalidOperationException(Resources.DISPATCH_NOT_ALLOWED);

            if (ifaceMethod.IsGenericMethodDefinition)
            {
                Ensure.Parameter.IsNotNull(genericArgs, nameof(genericArgs));

                ifaceMethod = ifaceMethod.MakeGenericMethod(genericArgs);
            }

            return Dispatch(ifaceMethod, args);
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

            FInterfaceMapping = GetInterfaceMapping();
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

                Dispatch(ifaceMethod: GetMethod(i => i.Dispose()));

                Assert(!FChildren.Any());

                FLock.Dispose();
            }

            base.Dispose(disposeManaged);
        }

        /// <summary>
        /// Disposal logic related to this class.
        /// </summary>
        [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "DisposeAsync() call is just an expression")]
        protected async override ValueTask AsyncDispose()
        {
            FParent?.Children.Remove(Self);

            await Task.WhenAll
            (
                Dispatch(ifaceMethod: GetMethod(i => i.DisposeAsync()))
                    .Cast<ValueTask>()
                    .Select(t => t.AsTask())
            );

            Assert(!FChildren.Any());

            FLock.Dispose();

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

                using (FLock.AcquireReaderLock()) 
                {
                    return FChildren.Count;
                }
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

            using (FLock.AcquireWriterLock())
            {
                if (FChildren.Count == MaxChildCount)
                    throw new InvalidOperationException(string.Format(Resources.Culture, Resources.TOO_MANY_CHILDREN, MaxChildCount));

                bool succeeded = FChildren.Add(child);
                Assert(succeeded, "Child already contained");
            }

            child.Parent = Self;
        }

        [CanSetParent]
        [MethodImpl(MethodImplOptions.NoInlining)]
        bool ICollection<TInterface>.Remove(TInterface child)
        {
            Ensure.Parameter.IsNotNull(child, nameof(child));
            CheckNotDisposed();

            if (child.Parent != Self) return false;

            using (FLock.AcquireWriterLock())
            {
                bool succeeded = FChildren.Remove(child);
                Assert(succeeded, "Child already removed");
            }

            child.Parent = null;

            return true;
        }

        bool ICollection<TInterface>.Contains(TInterface child) 
        {
            Ensure.Parameter.IsNotNull(child, nameof(child));
            CheckNotDisposed();

            return child.Parent == Self;
        }

        [CanSetParent]
        [MethodImpl(MethodImplOptions.NoInlining)]
        void ICollection<TInterface>.Clear()
        {
            CheckNotDisposed();

            using (FLock.AcquireWriterLock())
            {
                foreach (TInterface child in FChildren)
                {
                    child.Parent = null;
                }

                FChildren.Clear();
            }
        }

        void ICollection<TInterface>.CopyTo(TInterface[] array, int arrayIndex)
        {
            Ensure.Parameter.IsNotNull(array, nameof(array));
            CheckNotDisposed();

            using (FLock.AcquireReaderLock())
            {
                FChildren.CopyTo(array, arrayIndex);
            }
        }

        IEnumerator<TInterface> IEnumerable<TInterface>.GetEnumerator()
        {
            CheckNotDisposed();

            return new SafeEnumerator<TInterface>(FChildren, FLock);
        }

        IEnumerator IEnumerable.GetEnumerator() => Children.GetEnumerator();
        #endregion
    }
}