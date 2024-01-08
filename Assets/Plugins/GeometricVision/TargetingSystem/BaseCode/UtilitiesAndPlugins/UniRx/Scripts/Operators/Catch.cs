using System;
using System.Collections.Generic;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Schedulers;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class CatchObservable<T, TException> : OperatorObservableBase<T>
        where TException : Exception
    {
        readonly IObservable<T> source;
        readonly Func<TException, IObservable<T>> errorHandler;

        public CatchObservable(IObservable<T> source, Func<TException, IObservable<T>> errorHandler)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.errorHandler = errorHandler;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return new Catch(this, observer, cancel).Run();
        }

        class Catch : OperatorObserverBase<T, T>
        {
            readonly CatchObservable<T, TException> parent;
            SingleAssignmentDisposable sourceSubscription;
            SingleAssignmentDisposable exceptionSubscription;

            public Catch(CatchObservable<T, TException> parent, IObserver<T> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.sourceSubscription = new SingleAssignmentDisposable();
                this.exceptionSubscription = new SingleAssignmentDisposable();

                this.sourceSubscription.Disposable = this.parent.source.Subscribe(this);
                return StableCompositeDisposable.Create(this.sourceSubscription, this.exceptionSubscription);
            }

            public override void OnNext(T value)
            {
                this.observer.OnNext(value);
            }

            public override void OnError(Exception error)
            {
                var e = error as TException;
                if (e != null)
                {
                    IObservable<T> next;
                    try
                    {
                        if (this.parent.errorHandler == Stubs.CatchIgnore<T>)
                        {
                            next = Observable.Empty<T>(); // for avoid iOS AOT
                        }
                        else
                        {
                            next = this.parent.errorHandler(e);
                        }
                    }
                    catch (Exception ex)
                    {
                        try {
                            this.observer.OnError(ex); } finally {
                            this.Dispose(); };
                        return;
                    }

                    this.exceptionSubscription.Disposable = next.Subscribe(this.observer);
                }
                else
                {
                    try {
                        this.observer.OnError(error); } finally {
                        this.Dispose(); };
                    return;
                }
            }

            public override void OnCompleted()
            {
                try {
                    this.observer.OnCompleted(); } finally {
                    this.Dispose(); };
            }
        }
    }


    internal class CatchObservable<T> : OperatorObservableBase<T>
    {
        readonly IEnumerable<IObservable<T>> sources;

        public CatchObservable(IEnumerable<IObservable<T>> sources)
            : base(true)
        {
            this.sources = sources;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return new Catch(this, observer, cancel).Run();
        }

        class Catch : OperatorObserverBase<T, T>
        {
            readonly CatchObservable<T> parent;
            readonly object gate = new object();
            bool isDisposed;
            IEnumerator<IObservable<T>> e;
            SerialDisposable subscription;
            Exception lastException;
            Action nextSelf;

            public Catch(CatchObservable<T> parent, IObserver<T> observer, IDisposable cancel)
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
                        if (this.lastException != null)
                        {
                            try {
                                this.observer.OnError(this.lastException); }
                            finally {
                                this.Dispose(); }
                        }
                        else
                        {
                            try {
                                this.observer.OnCompleted(); }
                            finally {
                                this.Dispose(); }
                        }
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
                this.lastException = error;
                this.nextSelf();
            }

            public override void OnCompleted()
            {
                try {
                    this.observer.OnCompleted(); }
                finally {
                    this.Dispose(); }
                return;
            }
        }
    }
}