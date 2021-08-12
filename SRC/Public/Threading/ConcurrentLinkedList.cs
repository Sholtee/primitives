﻿/********************************************************************************
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
    public class ConcurrentLinkedList<T>: ICollection<LinkedListNode<T>>
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

        /// <inheritdoc/>
        public int Count => FCount;

        /// <inheritdoc/>
        public bool IsReadOnly { get; }

        /// <inheritdoc/>
        public void Add(LinkedListNode<T> item)
        {
            Ensure.Parameter.IsNotNull(item, nameof(item));

            if (item.Owner is not null)
                throw new ArgumentException(Resources.ALREADY_OWNED, nameof(item));

            for (; ; )
            {
                if (!Head.TryLock())
                    continue;

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

                item.Next = item.Prev = Head;
                Head.Next = Head.Prev = item;
                item.Owner = this;

                Head.Release();
            }
            else 
            {
                item.Next = Head.Next;
                item.Prev = Head;
                item.Next.Prev = item;
                item.Prev.Next = item;
                item.Owner = this;

                item.Prev.Release();
                item.Next.Release();
            }

            Increment(ref FCount);
        }

        /// <inheritdoc/>
        public bool Remove(LinkedListNode<T> item)
        {
            Ensure.Parameter.IsNotNull(item, nameof(item));

            if (item.Owner != this) 
                return false;

            //
            // INFO:
            //   Ez nem tamogatja azt az esetet ha ugyanazt az elemet akarnank parhuzamosan eltavolitani.
            //

            for (; ; )
            {
                if (!item.TryLock(allowRecursive: false))
                    continue;

                if (!item.Prev!.TryLock())
                {
                    item.Release();
                    continue;
                }

                if (!item.Next!.TryLock())
                {
                    item.Prev.Release();
                    item.Release();
                    continue;
                }

                break;
            }

            item.Next.Prev = item.Prev;
            item.Prev.Next = item.Next;

            if (item.Prev == Head && item.Next == Head)
                //
                // Ez azert van kulon hogy Head-en ne legyen a Release() ketszer hivva (ezzel potencialisan
                // asszertacios hibat okozva ha az elso Release() utan mar vki lock-olja a fejlecet).
                //

                Head.Release();
            else 
            {
                item.Prev.Release();
                item.Next.Release();
            }

            item.Invalidate();

            Decrement(ref FCount);
            return true;
        }

        /// <inheritdoc/>
        public void Clear() => throw new NotImplementedException();

        /// <inheritdoc/>
        public bool Contains(LinkedListNode<T> item) => Ensure.Parameter.IsNotNull(item, nameof(item)).Owner == this;

        /// <inheritdoc/>
        public void CopyTo(LinkedListNode<T>[] array, int arrayIndex)
        {
            Ensure.Parameter.IsNotNull(array, nameof(array));

            foreach (LinkedListNode<T> node in this)
            {
                array[arrayIndex++] = node;
            }
        }

        /// <inheritdoc/>
        public IEnumerator<LinkedListNode<T>> GetEnumerator() => new Enumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private sealed class Enumerator : Disposable, IEnumerator<LinkedListNode<T>>
        {
            public ConcurrentLinkedList<T> Owner { get; }

            public LinkedListNode<T> Current { get; private set; }

            object IEnumerator.Current => Current;

            #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            public Enumerator(ConcurrentLinkedList<T> owner) => Owner = owner;
            #pragma warning restore CS8618

            public bool MoveNext()
            {
                LinkedListNode<T> head = Owner.Head;

                for (; ; )
                {
                    if (Current is null)
                    {
                        if (!head.TryLock())
                            continue;

                        Current = head;
                    }

                    Assert(Current.LockedBy == Thread.CurrentThread.ManagedThreadId, "Current item is not owned");

                    if (!Current.Next!.TryLock())
                        continue;

                    break;
                }

                Current = Current.Next;

                //
                // Eresszuk el az elozo iteracioban zarolt node-ot
                //

                Current.Prev!.Release();

                return Current != head;
            }

            public void Reset() => throw new NotImplementedException();

            protected override void Dispose(bool disposeManaged)
            {
                if (disposeManaged)
                {
                    Current?.Release();
                }

                base.Dispose(disposeManaged);
            }
        }
    }
}