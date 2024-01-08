// this code is borrowed from RxOfficial(rx.codeplex.com) and modified

using System;
using System.Collections.Generic;
using System.Threading;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.InternalUtil
{
    internal class PriorityQueue<T> where T : IComparable<T>
    {
        private static long _count = long.MinValue;

        private IndexedItem[] _items;
        private int _size;

        public PriorityQueue()
            : this(16)
        {
        }

        public PriorityQueue(int capacity)
        {
            this._items = new IndexedItem[capacity];
            this._size = 0;
        }

        private bool IsHigherPriority(int left, int right)
        {
            return this._items[left].CompareTo(this._items[right]) < 0;
        }

        private void Percolate(int index)
        {
            if (index >= this._size || index < 0)
                return;
            var parent = (index - 1) / 2;
            if (parent < 0 || parent == index)
                return;

            if (this.IsHigherPriority(index, parent))
            {
                var temp = this._items[index];
                this._items[index] = this._items[parent];
                this._items[parent] = temp;
                this.Percolate(parent);
            }
        }

        private void Heapify()
        {
            this.Heapify(0);
        }

        private void Heapify(int index)
        {
            if (index >= this._size || index < 0)
                return;

            var left = 2 * index + 1;
            var right = 2 * index + 2;
            var first = index;

            if (left < this._size && this.IsHigherPriority(left, first))
                first = left;
            if (right < this._size && this.IsHigherPriority(right, first))
                first = right;
            if (first != index)
            {
                var temp = this._items[index];
                this._items[index] = this._items[first];
                this._items[first] = temp;
                this.Heapify(first);
            }
        }

        public int Count { get { return this._size; } }

        public T Peek()
        {
            if (this._size == 0)
                throw new InvalidOperationException("HEAP is Empty");

            return this._items[0].Value;
        }

        private void RemoveAt(int index)
        {
            this._items[index] = this._items[--this._size];
            this._items[this._size] = default(IndexedItem);
            this.Heapify();
            if (this._size < this._items.Length / 4)
            {
                var temp = this._items;
                this._items = new IndexedItem[this._items.Length / 2];
                Array.Copy(temp, 0, this._items, 0, this._size);
            }
        }

        public T Dequeue()
        {
            var result = this.Peek();
            this.RemoveAt(0);
            return result;
        }

        public void Enqueue(T item)
        {
            if (this._size >= this._items.Length)
            {
                var temp = this._items;
                this._items = new IndexedItem[this._items.Length * 2];
                Array.Copy(temp, this._items, temp.Length);
            }

            var index = this._size++;
            this._items[index] = new IndexedItem { Value = item, Id = Interlocked.Increment(ref _count) };
            this.Percolate(index);
        }

        public bool Remove(T item)
        {
            for (var i = 0; i < this._size; ++i)
            {
                if (EqualityComparer<T>.Default.Equals(this._items[i].Value, item))
                {
                    this.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        struct IndexedItem : IComparable<IndexedItem>
        {
            public T Value;
            public long Id;

            public int CompareTo(IndexedItem other)
            {
                var c = this.Value.CompareTo(other.Value);
                if (c == 0)
                    c = this.Id.CompareTo(other.Id);
                return c;
            }
        }
    }
}