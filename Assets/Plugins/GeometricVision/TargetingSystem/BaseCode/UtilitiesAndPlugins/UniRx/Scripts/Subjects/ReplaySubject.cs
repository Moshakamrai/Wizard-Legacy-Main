using System;
using System.Collections.Generic;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.InternalUtil;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Schedulers;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Subjects
{
    public sealed class ReplaySubject<T> : ISubject<T>, IOptimizedObservable<T>, IDisposable
    {
        object observerLock = new object();

        bool isStopped;
        bool isDisposed;
        Exception lastError;
        IObserver<T> outObserver = EmptyObserver<T>.Instance;

        readonly int bufferSize;
        readonly TimeSpan window;
        readonly DateTimeOffset startTime;
        readonly IScheduler scheduler;
        Queue<TimeInterval<T>> queue = new Queue<TimeInterval<T>>();


        public ReplaySubject()
            : this(int.MaxValue, TimeSpan.MaxValue, Scheduler.DefaultSchedulers.Iteration)
        {
        }

        public ReplaySubject(IScheduler scheduler)
            : this(int.MaxValue, TimeSpan.MaxValue, scheduler)
        {
        }

        public ReplaySubject(int bufferSize)
            : this(bufferSize, TimeSpan.MaxValue, Scheduler.DefaultSchedulers.Iteration)
        {
        }

        public ReplaySubject(int bufferSize, IScheduler scheduler)
            : this(bufferSize, TimeSpan.MaxValue, scheduler)
        {
        }

        public ReplaySubject(TimeSpan window)
            : this(int.MaxValue, window, Scheduler.DefaultSchedulers.Iteration)
        {
        }

        public ReplaySubject(TimeSpan window, IScheduler scheduler)
            : this(int.MaxValue, window, scheduler)
        {
        }

        // full constructor
        public ReplaySubject(int bufferSize, TimeSpan window, IScheduler scheduler)
        {
            if (bufferSize < 0) throw new ArgumentOutOfRangeException("bufferSize");
            if (window < TimeSpan.Zero) throw new ArgumentOutOfRangeException("window");
            if (scheduler == null) throw new ArgumentNullException("scheduler");

            this.bufferSize = bufferSize;
            this.window = window;
            this.scheduler = scheduler;
            this.startTime = scheduler.Now;
        }

        void Trim()
        {
            var elapsedTime = Scheduler.Normalize(this.scheduler.Now - this.startTime);

            while (this.queue.Count > this.bufferSize)
            {
                this.queue.Dequeue();
            }
            while (this.queue.Count > 0 && elapsedTime.Subtract(this.queue.Peek().Interval).CompareTo(this.window) > 0)
            {
                this.queue.Dequeue();
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
                this.Trim();
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
                this.Trim();
            }

            old.OnError(error);
        }

        public void OnNext(T value)
        {
            IObserver<T> current;
            lock (this.observerLock)
            {
                this.ThrowIfDisposed();
                if (this.isStopped) return;

                // enQ
                this.queue.Enqueue(new TimeInterval<T>(value, this.scheduler.Now - this.startTime));
                this.Trim();

                current = this.outObserver;
            }

            current.OnNext(value);
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (observer == null) throw new ArgumentNullException("observer");

            var ex = default(Exception);
            var subscription = default(Subscription);

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

                    subscription = new Subscription(this, observer);
                }

                ex = this.lastError;
                this.Trim();
                foreach (var item in this.queue)
                {
                    observer.OnNext(item.Value);
                }
            }

            if (subscription != null)
            {
                return subscription;
            }
            else if (ex != null)
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
                this.lastError = null;
                this.queue = null;
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
            ReplaySubject<T> parent;
            IObserver<T> unsubscribeTarget;

            public Subscription(ReplaySubject<T> parent, IObserver<T> unsubscribeTarget)
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