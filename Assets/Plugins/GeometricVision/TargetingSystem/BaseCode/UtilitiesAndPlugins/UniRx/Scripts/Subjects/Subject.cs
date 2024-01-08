using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.InternalUtil;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Subjects
{
    public sealed class Subject<T> : ISubject<T>, IDisposable, IOptimizedObservable<T>
    {
        object observerLock = new object();

        bool isStopped;
        bool isDisposed;
        Exception lastError;
        IObserver<T> outObserver = EmptyObserver<T>.Instance;

        public bool HasObservers
        {
            get
            {
                return !(this.outObserver is EmptyObserver<T>) && !this.isStopped && !this.isDisposed;
            }
        }

        public void OnCompleted()
        {
            IObserver<T> old;
            lock (this.observerLock)
            {
                this.ThrowIfDisposed();
                if (this.isStopped) return;

                old = this.outObserver;
                this.outObserver = EmptyObserver<T>.Instance;
                this.isStopped = true;
            }

            old.OnCompleted();
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
            this.outObserver.OnNext(value);
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (observer == null) throw new ArgumentNullException("observer");

            var ex = default(Exception);

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
            }

            if (ex != null)
            {
                observer.OnError(ex);
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
            Subject<T> parent;
            IObserver<T> unsubscribeTarget;

            public Subscription(Subject<T> parent, IObserver<T> unsubscribeTarget)
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
    }
}