/********************************************************************************
* RedBlackTree.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;

using static System.Diagnostics.Debug;

namespace Solti.Utils.Primitives
{
    //
    // Most of this file is taken from here:
    // https://github.com/dotnet/runtime/blob/43dd0a74ab524278620d8c6a9d33a9b73b2d2228/src/libraries/System.Collections/src/System/Collections/Generic/SortedSet.cs#L1
    //

    /// <summary>
    /// Specifies the color of a red-black tree node
    /// </summary>
    public enum NodeColor
    {
        /// <summary>
        /// Default value
        /// </summary>
        Unspecified,

        /// <summary>
        /// Black
        /// </summary>
        Black,

        /// <summary>
        /// Red
        /// </summary>
        Red
    }

    /// <summary>
    /// Represents a red-black tree node.
    /// </summary>
    public class RedBlackTreeNode<TData>
    {
        /// <summary>
        /// Creates a new node.
        /// </summary>
        public RedBlackTreeNode(TData data, NodeColor color = NodeColor.Unspecified)
        {
            Color = color;
            Data = data;
        }

        /// <summary>
        /// The left child of this node.
        /// </summary>
        public RedBlackTreeNode<TData>? Left { get; internal set; }

        /// <summary>
        /// The right child of this node.
        /// </summary>
        public RedBlackTreeNode<TData>? Right { get; internal set; }

        /// <summary>
        /// The associated data.
        /// </summary>
        public TData Data { get; }

        /// <summary>
        /// The color of this node.
        /// </summary>
        public NodeColor Color { get; internal set; }

        internal bool Is4Node => Left?.Color is NodeColor.Red && Right?.Color is NodeColor.Red;

        /// <summary>
        /// Clones the whole hierarchy starting from this node.
        /// </summary>
        public RedBlackTreeNode<TData> DeepClone()
        {
            RedBlackTreeNode<TData> newRoot = ShallowClone();

            Stack<(RedBlackTreeNode<TData> source, RedBlackTreeNode<TData> target)> pendingNodes = new();
            pendingNodes.Push((this, newRoot));

            while (pendingNodes.Count > 0)
            {
                (RedBlackTreeNode<TData> source, RedBlackTreeNode<TData> target) = pendingNodes.Pop();

                RedBlackTreeNode<TData> clonedNode;

                if (source.Left is not null)
                {
                    clonedNode = source.Left.ShallowClone();
                    target.Left = clonedNode;
                    pendingNodes.Push((source.Left, clonedNode));
                }

                if (source.Right is not null)
                {
                    clonedNode = source.Right.ShallowClone();
                    target.Right = clonedNode;
                    pendingNodes.Push((source.Right, clonedNode));
                }
            }

            return newRoot;
        }

        /// <summary>
        /// Clones the actual node only.
        /// </summary>
        public RedBlackTreeNode<TData> ShallowClone() => new(Data, Color);

        internal void Split4Node()
        {
            Assert(Left is not null);
            Assert(Right is not null);

            Color = NodeColor.Red;
            Right!.Color = Left!.Color = NodeColor.Black;
        }

        internal RedBlackTreeNode<TData> RotateLeft()
        {
            RedBlackTreeNode<TData> child = Right!;

            Right = child.Left;
            child.Left = this;
            return child;
        }

        internal RedBlackTreeNode<TData> RotateLeftRight()
        {
            RedBlackTreeNode<TData>
                child = Left!,
                grandChild = child.Right!;

            Left = grandChild.Right;
            grandChild.Right = this;
            child.Right = grandChild.Left;
            grandChild.Left = child;
            return grandChild;
        }

        internal RedBlackTreeNode<TData> RotateRight()
        {
            RedBlackTreeNode<TData> child = Left!;

            Left = child.Right;
            child.Right = this;
            return child;
        }

        internal RedBlackTreeNode<TData> RotateRightLeft()
        {
            RedBlackTreeNode<TData>
                child = Right!,
                grandChild = child.Left!;

            Right = grandChild.Left;
            grandChild.Left = this;
            child.Left = grandChild.Right;
            grandChild.Right = child;
            return grandChild;
        }

        internal void ReplaceChild(RedBlackTreeNode<TData> child, RedBlackTreeNode<TData> newChild)
        {
            Assert(HasChild(child));

            if (Left == child)
                Left = newChild;
            else
                Right = newChild;
        }

        internal bool HasChild(RedBlackTreeNode<TData> child) => child == Left || child == Right;
    }

    /// <summary>
    /// Represents a generic red-black tree
    /// </summary>
    public class RedBlackTree<TData> : IEnumerable<RedBlackTreeNode<TData>>
    {
        /// <summary>
        /// The number of leaves.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// The root of this tree.
        /// </summary>
        public RedBlackTreeNode<TData>? Root { get; private set; }

        /// <summary>
        /// The related comparer..
        /// </summary>
        public IComparer<TData> Comparer { get; }

        /// <summary>
        /// Creates a new <see cref="RedBlackTree{TData}"/> instance.
        /// </summary>
        public RedBlackTree(IComparer<TData> comparer) => Comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));

        /// <summary>
        /// Creates a new tree containing the new <paramref name="data"/>.
        /// </summary>
        public RedBlackTree<TData> With(TData data) => With(new RedBlackTreeNode<TData>(data));

        /// <summary>
        /// Creates a new tree containing the new <paramref name="node"/>.
        /// </summary>
        public RedBlackTree<TData> With(RedBlackTreeNode<TData> node)
        {
            if (node is null)
                throw new ArgumentNullException(nameof(node));

            if (Root is null)
                return new RedBlackTree<TData>(Comparer) 
                { 
                    Root = node,
                    Count = 1
                };

            RedBlackTree<TData> result = new(Comparer) 
            { 
                Root = Root.DeepClone(),
                Count = Count
            };

            if (!result.Add(node))
                throw new InvalidOperationException();

            return result;
        }

        /// <summary>
        /// Adds a new <paramref name="data"/> to this tree.
        /// </summary>
        public bool Add(TData data) => Add(new RedBlackTreeNode<TData>(data));

        /// <summary>
        /// Adds a new <paramref name="node"/> to this tree.
        /// </summary>
        public bool Add(RedBlackTreeNode<TData> node)
        {
            if (node is null)
                throw new ArgumentNullException(nameof(node));

            if (Root is null)
            {
                node.Color = NodeColor.Black;
                Root = node;
                Count = 1;
                return true;
            }

            //
            // Search for a node at bottom to insert the new node.
            // If we can guarantee the node we found is not a 4-node, it would be easy to do insertion.
            // We split 4-nodes along the search path.
            //

            RedBlackTreeNode<TData>?
                current = Root,
                parent = null,
                grandParent = null,
                greatGrandParent = null;

            int order = 0;
            while (current is not null)
            {
                order = Comparer.Compare(node.Data, current.Data);
                if (order is 0)
                {
                    //
                    // We could have changed root node to red during the search process.
                    // We need to set it to black before we return.
                    //

                    Root.Color = NodeColor.Black;
                    return false;
                }

                if (current.Is4Node)
                {
                    current.Split4Node();

                    //
                    // We could have introduced two consecutive red nodes after split. Fix that by rotation.
                    //

                    if (parent?.Color is NodeColor.Red)
                        InsertionBalance(current, ref parent!, grandParent!, greatGrandParent!);
                }

                greatGrandParent = grandParent;
                grandParent = parent;
                parent = current;
                current = order < 0
                    ? current.Left 
                    : current.Right;
            }

            Assert(parent is not null);

            //
            // We're ready to insert the new node.
            //

            node.Color = NodeColor.Red;
            if (order > 0)
                parent!.Right = node;
            else
                parent!.Left = node;

            //
            // The new node will be red, so we will need to adjust colors if its parent is also red.
            //

            if (parent.Color is NodeColor.Red)
                InsertionBalance(node, ref parent!, grandParent!, greatGrandParent!);

            //
            // The root node is always black.
            //

            Root.Color = NodeColor.Black;
            Count++;
            return true;
        }

        private void InsertionBalance(RedBlackTreeNode<TData> current, ref RedBlackTreeNode<TData> parent, RedBlackTreeNode<TData> grandParent, RedBlackTreeNode<TData> greatGrandParent)
        {
            Assert(parent is not null);
            Assert(grandParent is not null);

            bool
                parentIsOnRight = grandParent!.Right == parent,
                currentIsOnRight = parent!.Right == current;

            RedBlackTreeNode<TData> newChildOfGreatGrandParent;
            if (parentIsOnRight == currentIsOnRight)
                //
                // Same orientation, single rotation
                //

                newChildOfGreatGrandParent = currentIsOnRight 
                    ? grandParent.RotateLeft()
                    : grandParent.RotateRight();
            else
            {
                //
                // Different orientation, double rotation
                //

                newChildOfGreatGrandParent = currentIsOnRight
                    ? grandParent.RotateLeftRight()
                    : grandParent.RotateRightLeft();
                //
                // Current node now becomes the child of `greatGrandParent`
                //

                parent = greatGrandParent;
            }

            //
            // `grandParent` will become a child of either `parent` of `current`.
            //

            grandParent.Color = NodeColor.Red;
            newChildOfGreatGrandParent.Color = NodeColor.Black;

            ReplaceChildOrRoot(greatGrandParent, grandParent, newChildOfGreatGrandParent);
        }

        private void ReplaceChildOrRoot(RedBlackTreeNode<TData>? parent, RedBlackTreeNode<TData> child, RedBlackTreeNode<TData> newChild)
        {
            if (parent is not null)
                parent.ReplaceChild(child, newChild);
            else
                Root = newChild;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the nodes.
        /// </summary>
        public IEnumerator<RedBlackTreeNode<TData>> GetEnumerator() => new Enumertor(Root);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private sealed class Enumertor : IEnumerator<RedBlackTreeNode<TData>>
        {
            public RedBlackTreeNode<TData>? Root { get; }

            public RedBlackTreeNode<TData> Current { get; private set; }

            object IEnumerator.Current => Current;

            private readonly Stack<RedBlackTreeNode<TData>> FStack = new();

            #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            public Enumertor(RedBlackTreeNode<TData>? root)
            #pragma warning restore CS8618
            {
                Root = root;

                RedBlackTreeNode<TData>? node = Root;

                while (node is not null)
                {
                    FStack.Push(node);
                    node = node.Left;
                }
            }

            public bool MoveNext()
            {

                if (FStack.Count is 0)
                {
                    Current = null!;
                    return false;
                }

                Current = FStack.Pop();
                RedBlackTreeNode<TData>? node = Current.Right;
 
                while (node is not null)
                {
                    FStack.Push(node);
                    node = node.Left;
                }

                return true;
            }

            public void Reset() => throw new NotImplementedException();

            public void Dispose() {}
        }
    }
}
