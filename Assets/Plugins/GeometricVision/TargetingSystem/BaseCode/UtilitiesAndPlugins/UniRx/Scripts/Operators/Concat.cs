using System;
using System.Collections.Generic;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Schedulers;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    // needs to more improvement

    internal class ConcatObservable<T> : OperatorObservableBase<T>
    {
        readonly IEnumerable<IObservable<T>> sources;

        public ConcatObservable(IEnumerable<IObservable<T>> sources)
            : base(true)
        {
            this.sources = sources;
        }

        public IObservable<T> Combine(IEnumerable<IObservable<T>> combineSources)
        {
            return new ConcatObservable<T>(CombineSources(this.sources, combineSources));
        }

        static IEnumerable<IObservable<T>> CombineSources(IEnumerable<IObservable<T>> first, IEnumerable<IObservable<T>> second)
        {
            foreach (var item in first)
            {
                yield return item;
            }
            foreach (var item in second)
            {
                yield return item;
            }
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return new Concat(this, observer, cancel).Run();
        }

        class Concat : OperatorObserverBase<T, T>
        {
            readonly ConcatObservable<T> parent;
            readonly object gate = new object();

            bool isDisposed;
            IEnumerator<IObservable<T>> e;
            SerialDisposable subscription;
            Action nextSelf;

            public Concat(ConcatObservable<T> parent, IObserver<T> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.isDisposed = false;
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

                    var source = current;
                    var d = new SingleAssignmentDisposable();
                    this.subscription.Disposable = d;
                    d.Disposable = source.Subscribe(this);
                }
            }

            public override void OnNext(T value)
            {
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
                this.nextSelf();
            }
        }
    }
}
