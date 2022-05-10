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
    public class RedBlackTreeNode
    {
        /// <summary>
        /// Creates a new node.
        /// </summary>
        public RedBlackTreeNode(NodeColor color) => Color = color;

        /// <summary>
        /// The left child of this node.
        /// </summary>
        public RedBlackTreeNode? Left { get; internal set; }

        /// <summary>
        /// The right child of this node.
        /// </summary>
        public RedBlackTreeNode? Right { get; internal set; }

        /// <summary>
        /// The color of this node.
        /// </summary>
        public NodeColor Color { get; internal set; }

        internal bool Is4Node => Left?.Color is NodeColor.Red && Right?.Color is NodeColor.Red;

        /// <summary>
        /// Clones the whole hierarchy starting from this node.
        /// </summary>
        public RedBlackTreeNode DeepClone()
        {
            RedBlackTreeNode newRoot = ShallowClone();

            Stack<(RedBlackTreeNode source, RedBlackTreeNode target)> pendingNodes = new();
            pendingNodes.Push((this, newRoot));

            while (pendingNodes.Count > 0)
            {
                (RedBlackTreeNode source, RedBlackTreeNode target) = pendingNodes.Pop();

                RedBlackTreeNode clonedNode;

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
        public virtual RedBlackTreeNode ShallowClone() => new(Color);

        internal void Split4Node()
        {
            Assert(Left is not null);
            Assert(Right is not null);

            Color = NodeColor.Red;
            Right!.Color = Left!.Color = NodeColor.Black;
        }

        internal RedBlackTreeNode RotateLeft()
        {
            RedBlackTreeNode child = Right!;

            Right = child.Left;
            child.Left = this;
            return child;
        }

        internal RedBlackTreeNode RotateLeftRight()
        {
            RedBlackTreeNode
                child = Left!,
                grandChild = child.Right!;

            Left = grandChild.Right;
            grandChild.Right = this;
            child.Right = grandChild.Left;
            grandChild.Left = child;
            return grandChild;
        }

        internal RedBlackTreeNode RotateRight()
        {
            RedBlackTreeNode child = Left!;

            Left = child.Right;
            child.Right = this;
            return child;
        }

        internal RedBlackTreeNode RotateRightLeft()
        {
            RedBlackTreeNode 
                child = Right!,
                grandChild = child.Left!;

            Right = grandChild.Left;
            grandChild.Left = this;
            child.Left = grandChild.Right;
            grandChild.Right = child;
            return grandChild;
        }

        internal void ReplaceChild(RedBlackTreeNode child, RedBlackTreeNode newChild)
        {
            Assert(HasChild(child));

            if (Left == child)
                Left = newChild;
            else
                Right = newChild;
        }

        internal bool HasChild(RedBlackTreeNode child) => child == Left || child == Right;
    }

    /// <summary>
    /// Represents a generic red-black tree
    /// </summary>
    public class RedBlackTree<TNode>: IEnumerable<TNode> where TNode : RedBlackTreeNode
    {
        /// <summary>
        /// The number of leaves.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// The root of this tree.
        /// </summary>
        public TNode? Root { get; private set; }

        /// <summary>
        /// The related comparer..
        /// </summary>
        public IComparer<TNode> Comparer { get; }

        /// <summary>
        /// Creates a new <see cref="RedBlackTree{TNode}"/> instance.
        /// </summary>
        public RedBlackTree(IComparer<TNode> comparer) =>
            Comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));

        /// <summary>
        /// Creates a new tree containing the new node.
        /// </summary>
        public RedBlackTree<TNode> With(TNode node)
        {
            if (Root is null)
                return new RedBlackTree<TNode>(Comparer) 
                { 
                    Root = node,
                    Count = 1
                };

            RedBlackTree<TNode> result = new(Comparer) 
            { 
                Root = (TNode) Root.DeepClone(),
                Count = Count
            };

            if (!result.Add(node))
                throw new InvalidOperationException();

            return result;
        }

        /// <summary>
        /// Adds a new node to this tree.
        /// </summary>
        public bool Add(TNode node)
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

            TNode?
                current = Root,
                parent = null,
                grandParent = null,
                greatGrandParent = null;

            int order = 0;
            while (current is not null)
            {
                order = Comparer.Compare(node, current);
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
                    ? (TNode?) current.Left 
                    : (TNode?) current.Right;
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

        private void InsertionBalance(TNode current, ref TNode parent, TNode grandParent, TNode greatGrandParent)
        {
            Assert(parent is not null);
            Assert(grandParent is not null);

            bool
                parentIsOnRight = grandParent!.Right == parent,
                currentIsOnRight = parent!.Right == current;

            RedBlackTreeNode newChildOfGreatGrandParent;
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

            ReplaceChildOrRoot(greatGrandParent, grandParent, (TNode) newChildOfGreatGrandParent);
        }

        private void ReplaceChildOrRoot(TNode? parent, TNode child, TNode newChild)
        {
            if (parent is not null)
                parent.ReplaceChild(child, newChild);
            else
                Root = newChild;
        }

        /// <inheritdoc/>
        public IEnumerator<TNode> GetEnumerator() => new Enumertor(Root);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private sealed class Enumertor : IEnumerator<TNode>
        {
            public TNode? Root { get; }

            public TNode Current { get; private set; }

            object IEnumerator.Current => Current;

            private readonly Stack<TNode> FStack = new();

            #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            public Enumertor(TNode? root)
            #pragma warning restore CS8618
            {
                Root = root;

                TNode? node = Root;

                while (node is not null)
                {
                    FStack.Push(node);
                    node = (TNode?) node.Left;
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
                TNode? node = (TNode?) Current.Right;
 
                while (node is not null)
                {
                    FStack.Push(node);
                    node = (TNode?) node.Left;
                }

                return true;
            }

            public void Reset() => throw new NotImplementedException();

            public void Dispose() {}
        }
    }
}
