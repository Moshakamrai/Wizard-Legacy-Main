// This code is borrwed from Rx Official and some modified.

// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables
{
    /// <summary>
    /// Represents a disposable resource that only disposes its underlying disposable resource when all <see cref="GetDisposable">dependent disposable objects</see> have been disposed.
    /// </summary>
    public sealed class RefCountDisposable : ICancelable
    {
        private readonly object _gate = new object();
        private IDisposable _disposable;
        private bool _isPrimaryDisposed;
        private int _count;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Reactive.Disposables.RefCountDisposable"/> class with the specified disposable.
        /// </summary>
        /// <param name="disposable">Underlying disposable.</param>
        /// <exception cref="ArgumentNullException"><paramref name="disposable"/> is null.</exception>
        public RefCountDisposable(IDisposable disposable)
        {
            if (disposable == null)
                throw new ArgumentNullException("disposable");

            this._disposable = disposable;
            this._isPrimaryDisposed = false;
            this._count = 0;
        }

        /// <summary>
        /// Gets a value that indicates whether the object is disposed.
        /// </summary>
        public bool IsDisposed
        {
            get { return this._disposable == null; }
        }

        /// <summary>
        /// Returns a dependent disposable that when disposed decreases the refcount on the underlying disposable.
        /// </summary>
        /// <returns>A dependent disposable contributing to the reference count that manages the underlying disposable's lifetime.</returns>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Backward compat + non-trivial work for a property getter.")]
        public IDisposable GetDisposable()
        {
            lock (this._gate)
            {
                if (this._disposable == null)
                {
                    return Disposable.Empty;
                }
                else
                {
                    this._count++;
                    return new InnerDisposable(this);
                }
            }
        }

        /// <summary>
        /// Disposes the underlying disposable only when all dependent disposables have been disposed.
        /// </summary>
        public void Dispose()
        {
            var disposable = default(IDisposable);
            lock (this._gate)
            {
                if (this._disposable != null)
                {
                    if (!this._isPrimaryDisposed)
                    {
                        this._isPrimaryDisposed = true;

                        if (this._count == 0)
                        {
                            disposable = this._disposable;
                            this._disposable = null;
                        }
                    }
                }
            }

            if (disposable != null)
                disposable.Dispose();
        }

        private void Release()
        {
            var disposable = default(IDisposable);
            lock (this._gate)
            {
                if (this._disposable != null)
                {
                    this._count--;

                    if (this._isPrimaryDisposed)
                    {
                        if (this._count == 0)
                        {
                            disposable = this._disposable;
                            this._disposable = null;
                        }
                    }
                }
            }

            if (disposable != null)
                disposable.Dispose();
        }

        sealed class InnerDisposable : IDisposable
        {
            private RefCountDisposable _parent;
            object parentLock = new object();

            public InnerDisposable(RefCountDisposable parent)
            {
                this._parent = parent;
            }

            public void Dispose()
            {
                RefCountDisposable parent;
                lock (this.parentLock)
                {
                    parent = this._parent;
                    this._parent = null;
                }
                if (parent != null)
                    parent.Release();
            }
        }
    }

    public partial class Observable
    {
        static IObservable<T> AddRef<T>(IObservable<T> xs, RefCountDisposable r)
        {
            return Scripts.Observable.Create<T>((IObserver<T> observer) => new CompositeDisposable(new IDisposable[]
	        {
		        r.GetDisposable(),
		        xs.Subscribe(observer)
	        }));
        }
    }
}