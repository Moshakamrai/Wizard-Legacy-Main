using System;
using System.Collections.Generic;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Schedulers;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class RepeatSafeObservable<T> : OperatorObservableBase<T>
    {
        readonly IEnumerable<IObservable<T>> sources;

        public RepeatSafeObservable(IEnumerable<IObservable<T>> sources, bool isRequiredSubscribeOnCurrentThread)
            : base(isRequiredSubscribeOnCurrentThread)
        {
            this.sources = sources;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return new RepeatSafe(this, observer, cancel).Run();
        }

        class RepeatSafe : OperatorObserverBase<T, T>
        {
            readonly RepeatSafeObservable<T> parent;
            readonly object gate = new object();

            IEnumerator<IObservable<T>> e;
            SerialDisposable subscription;
            Action nextSelf;
            bool isDisposed;
            bool isRunNext;

            public RepeatSafe(RepeatSafeObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.isDisposed = false;
                this.isRunNext = false;
                this.e = this.parent.sources.GetEnumerator();
                this.subscription = new SerialDisposable();

                var schedule = Scheduler.DefaultSchedulers.TailRecursion.Schedule(this.RecursiveRun);

                return StableCompositeDisposable.Create(schedule, this.subscription, Disposable.Create(() =>
                {
                    lock (this.gate)
                    {
                        this.isDisposed = true;
                        this.e.Dispose();
                    }
                }));
            }

            void RecursiveRun(Action self)
            {
                lock (this.gate)
                {
                    this.nextSelf = self;
                    if (this.isDisposed) return;

                    var current = default(IObservable<T>);
                    var hasNext = false;
                    var ex = default(Exception);

                    try
                    {
                        hasNext = this.e.MoveNext();
                        if (hasNext)
                        {
                            current = this.e.Current;
                            if (current == null) throw new InvalidOperationException("sequence is null.");
                        }
                        else
                        {
                            this.e.Dispose();
                        }
                    }
                    catch (Exception exception)
                    {
                        ex = exception;
                        this.e.Dispose();
                    }

                    if (ex != null)
                    {
                        try {
                            this.observer.OnError(ex); }
                        finally {
                            this.Dispose(); }
                        return;
                    }

                    if (!hasNext)
                    {
                        try {
                            this.observer.OnCompleted(); }
                        finally {
                            this.Dispose(); }
                        return;
                    }

                    var source = this.e.Current;
                    var d = new SingleAssignmentDisposable();
                    this.subscription.Disposable = d;
                    d.Disposable = source.Subscribe(this);
                }
            }

            public override void OnNext(T value)
            {
                this.isRunNext = true;
                this.observer.OnNext(value);
            }
            public override void OnError(Exception error)
            {
                try {
                    this.observer.OnError(error); }
                finally {
                    this.Dispose(); }
            }

            public override void OnCompleted()
            {
                if (this.isRunNext && !this.isDisposed)
                {
                    this.isRunNext = false;
                    this.nextSelf();
                }
                else
                {
                    this.e.Dispose();
                    if (!this.isDisposed)
                    {
                        try {
                            this.observer.OnCompleted(); }
                        finally {
                            this.Dispose(); }
                    }
                }
            }
        }
    }
}