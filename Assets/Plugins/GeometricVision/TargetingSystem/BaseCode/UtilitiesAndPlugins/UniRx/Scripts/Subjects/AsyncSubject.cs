using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.InternalUtil;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;
#if (NET_4_6 || NET_STANDARD_2_0)

#endif

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Subjects
{
    public sealed class AsyncSubject<T> : ISubject<T>, IOptimizedObservable<T>, IDisposable
#if (NET_4_6 || NET_STANDARD_2_0)
        , INotifyCompletion
#endif
    {
        object observerLock = new object();

        T lastValue;
        bool hasValue;
        bool isStopped;
        bool isDisposed;
        Exception lastError;
        IObserver<T> outObserver = EmptyObserver<T>.Instance;

        public T Value
        {
            get
            {
                this.ThrowIfDisposed();
                if (!this.isStopped) throw new InvalidOperationException("AsyncSubject is not completed yet");
                if (this.lastError != null) this.lastError.Throw();
                return this.lastValue;
            }
        }

        public bool HasObservers
        {
            get
            {
                return !(this.outObserver is EmptyObserver<T>) && !this.isStopped && !this.isDisposed;
            }
        }

        public bool IsCompleted { get { return this.isStopped; } }

        public void OnCompleted()
        {
            IObserver<T> old;
            T v;
            bool hv;
            lock (this.observerLock)
            {
                this.ThrowIfDisposed();
                if (this.isStopped) return;

                old = this.outObserver;
                this.outObserver = EmptyObserver<T>.Instance;
                this.isStopped = true;
                v = this.lastValue;
                hv = this.hasValue;
            }

            if (hv)
            {
                old.OnNext(v);
                old.OnCompleted();
            }
            else
            {
                old.OnCompleted();
            }
        }

        public void OnError(Exception error)
        {
            if (error == null) throw new ArgumentNullException("error");

            IObserver<T> old;
            lock (this.observerLock)
            {
                this.ThrowIfDisposed();
                if (this.isStopped) return;

                old = this.outObserver;
                this.outObserver = EmptyObserver<T>.Instance;
                this.isStopped = true;
                this.lastError = error;
            }

            old.OnError(error);
        }

        public void OnNext(T value)
        {
            lock (this.observerLock)
            {
                this.ThrowIfDisposed();
                if (this.isStopped) return;

                this.hasValue = true;
                this.lastValue = value;
            }
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (observer == null) throw new ArgumentNullException("observer");

            var ex = default(Exception);
            var v = default(T);
            var hv = false;

            lock (this.observerLock)
            {
                this.ThrowIfDisposed();
                if (!this.isStopped)
                {
                    var listObserver = this.outObserver as ListObserver<T>;
                    if (listObserver != null)
                    {
                        this.outObserver = listObserver.Add(observer);
                    }
                    else
                    {
                        var current = this.outObserver;
                        if (current is EmptyObserver<T>)
                        {
                            this.outObserver = observer;
                        }
                        else
                        {
                            this.outObserver = new ListObserver<T>(new ImmutableList<IObserver<T>>(new[] { current, observer }));
                        }
                    }

                    return new Subscription(this, observer);
                }

                ex = this.lastError;
                v = this.lastValue;
                hv = this.hasValue;
            }

            if (ex != null)
            {
                observer.OnError(ex);
            }
            else if (hv)
            {
                observer.OnNext(v);
                observer.OnCompleted();
            }
            else
            {
                observer.OnCompleted();
            }

            return Disposable.Empty;
        }

        public void Dispose()
        {
            lock (this.observerLock)
            {
                this.isDisposed = true;
                this.outObserver = DisposedObserver<T>.Instance;
                this.lastError = null;
                this.lastValue = default(T);
            }
        }

        void ThrowIfDisposed()
        {
            if (this.isDisposed) throw new ObjectDisposedException("");
        }

        public bool IsRequiredSubscribeOnCurrentThread()
        {
            return false;
        }

        class Subscription : IDisposable
        {
            readonly object gate = new object();
            AsyncSubject<T> parent;
            IObserver<T> unsubscribeTarget;

            public Subscription(AsyncSubject<T> parent, IObserver<T> unsubscribeTarget)
            {
                this.parent = parent;
                this.unsubscribeTarget = unsubscribeTarget;
            }

            public void Dispose()
            {
                lock (this.gate)
                {
                    if (this.parent != null)
                    {
                        lock (this.parent.observerLock)
                        {
                            var listObserver = this.parent.outObserver as ListObserver<T>;
                            if (listObserver != null)
                            {
                                this.parent.outObserver = listObserver.Remove(this.unsubscribeTarget);
                            }
                            else
                            {
                                this.parent.outObserver = EmptyObserver<T>.Instance;
                            }

                            this.unsubscribeTarget = null;
                            this.parent = null;
                        }
                    }
                }
            }
        }


#if (NET_4_6 || NET_STANDARD_2_0)

        /// <summary>
        /// Gets an awaitable object for the current AsyncSubject.
        /// </summary>
        /// <returns>Object that can be awaited.</returns>
        public AsyncSubject<T> GetAwaiter()
        {
            return this;
        }

        /// <summary>
        /// Specifies a callback action that will be invoked when the subject completes.
        /// </summary>
        /// <param name="continuation">Callback action that will be invoked when the subject completes.</param>
        /// <exception cref="ArgumentNullException"><paramref name="continuation"/> is null.</exception>
        public void OnCompleted(Action continuation)
        {
            if (continuation == null)
                throw new ArgumentNullException("continuation");

            this.OnCompleted(continuation, true);
        }

         void OnCompleted(Action continuation, bool originalContext)
        {
            //
            // [OK] Use of unsafe Subscribe: this type's Subscribe implementation is safe.
            //
            this.Subscribe/*Unsafe*/(new AwaitObserver(continuation, originalContext));
        }

        class AwaitObserver : IObserver<T>
        {
            private readonly SynchronizationContext _context;
            private readonly Action _callback;

            public AwaitObserver(Action callback, bool originalContext)
            {
                if (originalContext) this._context = SynchronizationContext.Current;

                this._callback = callback;
            }

            public void OnCompleted()
            {
                this.InvokeOnOriginalContext();
            }

            public void OnError(Exception error)
            {
                this.InvokeOnOriginalContext();
            }

            public void OnNext(T value)
            {
            }

            private void InvokeOnOriginalContext()
            {
                if (this._context != null)
                {
                    //
                    // No need for OperationStarted and OperationCompleted calls here;
                    // this code is invoked through await support and will have a way
                    // to observe its start/complete behavior, either through returned
                    // Task objects or the async method builder's interaction with the
                    // SynchronizationContext object.
                    //
                    this._context.Post(c => ((Action)c)(), this._callback);
                }
                else
                {
                    this._callback();
                }
            }
        }

        /// <summary>
        /// Gets the last element of the subject, potentially blocking until the subject completes successfully or exceptionally.
        /// </summary>
        /// <returns>The last element of the subject. Throws an InvalidOperationException if no element was received.</returns>
        /// <exception cref="InvalidOperationException">The source sequence is empty.</exception>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Await pattern for C# and VB compilers.")]
        public T GetResult()
        {
            if (!this.isStopped)
            {
                var e = new ManualResetEvent(false);
                this.OnCompleted(() => e.Set(), false);
                e.WaitOne();
            }

            if (this.lastError != null)
            {
                this.lastError.Throw();
            }

            if (!this.hasValue)
                throw new InvalidOperationException("NO_ELEMENTS");

            return this.lastValue;
        }
#endif
    }
}
