using System;
using System.Collections.Generic;
// using System.Linq; do not use LINQ

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables
{
    // copy, modified from Rx Official

    public sealed class CompositeDisposable : ICollection<IDisposable>, IDisposable, ICancelable
    {
        private readonly object _gate = new object();

        private bool _disposed;
        private List<IDisposable> _disposables;
        private int _count;
        private const int SHRINK_THRESHOLD = 64;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Reactive.Disposables.CompositeDisposable"/> class with no disposables contained by it initially.
        /// </summary>
        public CompositeDisposable()
        {
            this._disposables = new List<IDisposable>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Reactive.Disposables.CompositeDisposable"/> class with the specified number of disposables.
        /// </summary>
        /// <param name="capacity">The number of disposables that the new CompositeDisposable can initially store.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than zero.</exception>
        public CompositeDisposable(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException("capacity");

            this._disposables = new List<IDisposable>(capacity);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Reactive.Disposables.CompositeDisposable"/> class from a group of disposables.
        /// </summary>
        /// <param name="disposables">Disposables that will be disposed together.</param>
        /// <exception cref="ArgumentNullException"><paramref name="disposables"/> is null.</exception>
        public CompositeDisposable(params IDisposable[] disposables)
        {
            if (disposables == null)
                throw new ArgumentNullException("disposables");

            this._disposables = new List<IDisposable>(disposables);
            this._count = this._disposables.Count;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Reactive.Disposables.CompositeDisposable"/> class from a group of disposables.
        /// </summary>
        /// <param name="disposables">Disposables that will be disposed together.</param>
        /// <exception cref="ArgumentNullException"><paramref name="disposables"/> is null.</exception>
        public CompositeDisposable(IEnumerable<IDisposable> disposables)
        {
            if (disposables == null)
                throw new ArgumentNullException("disposables");

            this._disposables = new List<IDisposable>(disposables);
            this._count = this._disposables.Count;
        }

        /// <summary>
        /// Gets the number of disposables contained in the CompositeDisposable.
        /// </summary>
        public int Count
        {
            get
            {
                return this._count;
            }
        }

        /// <summary>
        /// Adds a disposable to the CompositeDisposable or disposes the disposable if the CompositeDisposable is disposed.
        /// </summary>
        /// <param name="item">Disposable to add.</param>
        /// <exception cref="ArgumentNullException"><paramref name="item"/> is null.</exception>
        public void Add(IDisposable item)
        {
            if (item == null)
                throw new ArgumentNullException("item");

            var shouldDispose = false;
            lock (this._gate)
            {
                shouldDispose = this._disposed;
                if (!this._disposed)
                {
                    this._disposables.Add(item);
                    this._count++;
                }
            }
            if (shouldDispose)
                item.Dispose();
        }

        /// <summary>
        /// Removes and disposes the first occurrence of a disposable from the CompositeDisposable.
        /// </summary>
        /// <param name="item">Disposable to remove.</param>
        /// <returns>true if found; false otherwise.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="item"/> is null.</exception>
        public bool Remove(IDisposable item)
        {
            if (item == null)
                throw new ArgumentNullException("item");

            var shouldDispose = false;

            lock (this._gate)
            {
                if (!this._disposed)
                {
                    //
                    // List<T> doesn't shrink the size of the underlying array but does collapse the array
                    // by copying the tail one position to the left of the removal index. We don't need
                    // index-based lookup but only ordering for sequential disposal. So, instead of spending
                    // cycles on the Array.Copy imposed by Remove, we use a null sentinel value. We also
                    // do manual Swiss cheese detection to shrink the list if there's a lot of holes in it.
                    //
                    var i = this._disposables.IndexOf(item);
                    if (i >= 0)
                    {
                        shouldDispose = true;
                        this._disposables[i] = null;
                        this._count--;

                        if (this._disposables.Capacity > SHRINK_THRESHOLD && this._count < this._disposables.Capacity / 2)
                        {
                            var old = this._disposables;
                            this._disposables = new List<IDisposable>(this._disposables.Capacity / 2);

                            foreach (var d in old)
                                if (d != null)
                                    this._disposables.Add(d);
                        }
                    }
                }
            }

            if (shouldDispose)
                item.Dispose();

            return shouldDispose;
        }

        /// <summary>
        /// Disposes all disposables in the group and removes them from the group.
        /// </summary>
        public void Dispose()
        {
            var currentDisposables = default(IDisposable[]);
            lock (this._gate)
            {
                if (!this._disposed)
                {
                    this._disposed = true;
                    currentDisposables = this._disposables.ToArray();
                    this._disposables.Clear();
                    this._count = 0;
                }
            }

            if (currentDisposables != null)
            {
                foreach (var d in currentDisposables)
                    if (d != null)
                        d.Dispose();
            }
        }

        /// <summary>
        /// Removes and disposes all disposables from the CompositeDisposable, but does not dispose the CompositeDisposable.
        /// </summary>
        public void Clear()
        {
            var currentDisposables = default(IDisposable[]);
            lock (this._gate)
            {
                currentDisposables = this._disposables.ToArray();
                this._disposables.Clear();
                this._count = 0;
            }

            foreach (var d in currentDisposables)
                if (d != null)
                    d.Dispose();
        }

        /// <summary>
        /// Determines whether the CompositeDisposable contains a specific disposable.
        /// </summary>
        /// <param name="item">Disposable to search for.</param>
        /// <returns>true if the disposable was found; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="item"/> is null.</exception>
        public bool Contains(IDisposable item)
        {
            if (item == null)
                throw new ArgumentNullException("item");

            lock (this._gate)
            {
                return this._disposables.Contains(item);
            }
        }

        /// <summary>
        /// Copies the disposables contained in the CompositeDisposable to an array, starting at a particular array index.
        /// </summary>
        /// <param name="array">Array to copy the contained disposables to.</param>
        /// <param name="arrayIndex">Target index at which to copy the first disposable of the group.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than zero. -or - <paramref name="arrayIndex"/> is larger than or equal to the array length.</exception>
        public void CopyTo(IDisposable[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (arrayIndex < 0 || arrayIndex >= array.Length)
                throw new ArgumentOutOfRangeException("arrayIndex");

            lock (this._gate)
            {
                var disArray = new List<IDisposable>();
                foreach (var item in this._disposables)
                {
                    if (item != null) disArray.Add(item);
                }

                Array.Copy(disArray.ToArray(), 0, array, arrayIndex, array.Length - arrayIndex);
            }
        }

        /// <summary>
        /// Always returns false.
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the CompositeDisposable.
        /// </summary>
        /// <returns>An enumerator to iterate over the disposables.</returns>
        public IEnumerator<IDisposable> GetEnumerator()
        {
            var res = new List<IDisposable>();

            lock (this._gate)
            {
                foreach (var d in this._disposables)
                {
                    if (d != null) res.Add(d);
                }
            }

            return res.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the CompositeDisposable.
        /// </summary>
        /// <returns>An enumerator to iterate over the disposables.</returns>
        global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Gets a value that indicates whether the object is disposed.
        /// </summary>
        public bool IsDisposed
        {
            get { return this._disposed; }
        }
    }
}