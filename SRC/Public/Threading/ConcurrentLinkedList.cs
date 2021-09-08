/********************************************************************************
* ConcurrentLinkedList.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

using static System.Diagnostics.Debug;
using static System.Threading.Interlocked;

namespace Solti.Utils.Primitives.Threading
{
    using Patterns;
    using Properties;

    /// <summary>
    /// Represents a linked list node.
    /// </summary>
    public class LinkedListNode<T>
    {
        private LinkedListNode<T>? FPrev;

        /// <summary>
        /// The previous node.
        /// </summary>
        public LinkedListNode<T>? Prev
        {
            get => FPrev;
            internal set
            {
                Assert(FLockedBy is 0 || Thread.CurrentThread.ManagedThreadId == FLockedBy, "Attempt to write a not-owned node");
                FPrev = value;
            }
        }

        private LinkedListNode<T>? FNext;

        /// <summary>
        /// The next node.
        /// </summary>
        public LinkedListNode<T>? Next
        {
            get => FNext;
            internal set
            {
                Assert(FLockedBy is 0 || Thread.CurrentThread.ManagedThreadId == FLockedBy, "Attempt to write a not-owned node");
                FNext = value;
            }
        }

        /// <summary>
        /// The owner of this node.
        /// </summary>
        public ConcurrentLinkedList<T>? Owner { get; internal set; }

        private int FLockedBy;

        /// <summary>
        /// The id of the <see cref="Thread"/> that locked this node.
        /// </summary>
        public int LockedBy => FLockedBy;

        /// <summary>
        /// The value of this node
        /// </summary>
        public T? Value {get; init;}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryLock(bool allowRecursive = true)
        {
            int prev = CompareExchange(ref FLockedBy, Thread.CurrentThread.ManagedThreadId, 0);

            if (prev == Thread.CurrentThread.ManagedThreadId && !allowRecursive)
                throw new InvalidOperationException(Resources.RECURSIVE_LOCK);

            //
            // Ugyanaz a szal tobbszor is kerelmezheti a lock-ot (pl 0 hosszu listanal az elso elem
            // felvetelekor).
            //

            return prev == 0 || prev == Thread.CurrentThread.ManagedThreadId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Release()
        {
            //
            // Csak az a szal eresztheti el az elemet aki magat a lock-ot is kerelmezte.
            //

            int prev = CompareExchange(ref FLockedBy, 0, Thread.CurrentThread.ManagedThreadId);

            Assert(prev == Thread.CurrentThread.ManagedThreadId, "Attempt to release a not-owned node");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Invalidate()
        {
            Assert(FLockedBy == Thread.CurrentThread.ManagedThreadId, "Attempt to invalidate a not-owned node");

            Prev = Next = null;
            Owner = null;
            FLockedBy = 0;
        }
    }

    /// <summary>
    /// Represents a doubly linked list that can be shared across threads.
    /// </summary>
    [SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "The name is meaningful")]
    public class ConcurrentLinkedList<T> : IEnumerable<T>
    {
        private int FCount;

        /// <summary>
        /// The head of this instance.
        /// </summary>
        public LinkedListNode<T> Head { get; } = new LinkedListHead();

        private sealed class LinkedListHead : LinkedListNode<T>
        {
            public LinkedListHead()
            {
                Next = this;
                Prev = this;
            }
        }

        /// <summary>
        /// The number of elements stored in the list.
        /// </summary>
        public int Count => FCount;

        /// <summary>
        /// Ads an element to the beginning of the list.
        /// </summary>
        public LinkedListNode<T> AddFirst(T item)
        {
            LinkedListNode<T> node = new() { Value = item };

            for (; ; )
            {
                if (!Head.TryLock())
                    continue;

                //
                // Mivel Head zarolva van, ezert annak Next-jet biztosan nem irtak mire ide eljutunk
                //

                if (!Head.Next!.TryLock()) // ures listanal (Next == Head) ez nem csinal semmit 
                {
                    //
                    // TODO: [perf] Itt raprobalhatnank a Head.Prev-re hatha az nincs zarolva
                    //

                    Head.Release();
                    continue;
                }

                break;
            }

            if (Head == Head.Next)
            {
                //
                // Ez azert van kulon hogy Head-en ne legyen a Release() ketszer hivva (ezzel potencialisan
                // asszertacios hibat okozva ha az elso Release() utan mar vki lock-olja a fejlecet).
                //

                node.Next = node.Prev = Head;
                Head.Next = Head.Prev = node;
                node.Owner = this;

                Head.Release();
            }
            else
            {
                node.Next = Head.Next;
                node.Prev = Head;
                node.Next.Prev = node;
                node.Prev.Next = node;
                node.Owner = this;

                node.Prev.Release();
                node.Next.Release();
            }

            Increment(ref FCount);

            return node;
        }

        /// <summary>
        /// Removes the given <paramref name="node"/> from the list.
        /// </summary>
        public bool Remove(LinkedListNode<T> node)
        {
            Ensure.Parameter.IsNotNull(node, nameof(node));

            //
            // INFO:
            //   Ez nem tamogatja azt az esetet ha ugyanazt az elemet akarnank parhuzamosan eltavolitani.
            //

            for (; ; )
            {
                if (!node.TryLock(allowRecursive: false))
                    continue;

                //
                // Kozben a TryTake() mar eltavolitotta?
                //

                if (node.Owner != this)
                {
                    node.Invalidate();
                    return false;
                }

                if (!node.Prev!.TryLock())
                {
                    node.Release();
                    continue;
                }

                if (!node.Next!.TryLock())
                {
                    node.Prev.Release();
                    node.Release();
                    continue;
                }

                break;
            }

            node.Next.Prev = node.Prev;
            node.Prev.Next = node.Next;

            if (node.Prev == Head && node.Next == Head)
                //
                // Ez azert van kulon hogy Head-en ne legyen a Release() ketszer hivva (ezzel potencialisan
                // asszertacios hibat okozva ha az elso Release() utan mar vki lock-olja a fejlecet).
                //

                Head.Release();
            else
            {
                node.Prev.Release();
                node.Next.Release();
            }

            node.Invalidate();

            Decrement(ref FCount);
            return true;
        }

        /// <summary>
        /// Removes the first element from the list.
        /// </summary>
        public bool TakeFirst(out T item)
        {
            LinkedListNode<T> first;

            for (; ; )
            {
                if (!Head.TryLock())
                    continue;

                //
                // Mivel Head zarolva van ezert annak Next-jet biztosan nem modositottak mire ide eljutunk
                //

                first = Head.Next!;

                if (first == Head)
                {
                    Head.Release();

                    item = default!;
                    return false;
                }

                if (!first.TryLock())
                {
                    Head.Release();
                    continue;
                }

                //
                // Utolso elem?
                //

                if (first.Next == Head)
                {
                    item = first.Value!;
                    first.Invalidate();

                    Head.Next = Head;
                    Head.Prev = Head;
                    Head.Release();

                    return true;
                }

                if (!first.Next!.TryLock())
                {
                    Head.Next!.Release();
                    Head.Release();
                    continue;
                }

                break;
            }

            first.Next!.Prev = first.Prev;
            first.Prev!.Next = first.Next;

            first.Prev.Release();
            first.Next.Release();

            item = first.Value!;
            first.Invalidate();

            return true;
        }

        /// <summary>
        /// Clears the list.
        /// </summary>
        public void Clear()
        {
            while (TakeFirst(out _)) { }
        }

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator() => new Enumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private sealed class Enumerator: Disposable, IEnumerator<T>
        {
            public ConcurrentLinkedList<T> Owner { get; }

            public LinkedListNode<T>? CurrentNode { get; private set; }

            public T Current => (CurrentNode is not null ? CurrentNode.Value : default)!;

            object IEnumerator.Current => Current!;

            public Enumerator(ConcurrentLinkedList<T> owner) => Owner = owner;

            public bool MoveNext()
            {
                LinkedListNode<T> head = Owner.Head;

                for (; ; )
                {
                    if (CurrentNode is null)
                    {
                        if (!head.TryLock())
                            continue;

                        CurrentNode = head;
                    }

                    Assert(CurrentNode.LockedBy == Thread.CurrentThread.ManagedThreadId, "Current item is not owned");

                    if (!CurrentNode.Next!.TryLock())
                        continue;

                    break;
                }

                CurrentNode = CurrentNode.Next;

                //
                // Eresszuk el az elozo iteracioban zarolt node-ot
                //

                if (CurrentNode.Prev == CurrentNode)
                    Assert(CurrentNode == head, "'Current' must point to the list head");
                    //
                    // Head ne legyen duplan felszabaditva [Dispose() is fel fogja szabaditani]
                    //

                else
                    CurrentNode.Prev!.Release();

                return CurrentNode != head;
            }

            public void Reset() => throw new NotImplementedException();

            protected override void Dispose(bool disposeManaged)
            {
                if (disposeManaged)
                    CurrentNode?.Release();

                base.Dispose(disposeManaged);
            }
        }
    }
}
