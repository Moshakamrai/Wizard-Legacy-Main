using System;
using System.Collections.Generic;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Schedulers;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class ToObservableObservable<T> : OperatorObservableBase<T>
    {
        readonly IEnumerable<T> source;
        readonly IScheduler scheduler;

        public ToObservableObservable(IEnumerable<T> source, IScheduler scheduler)
            : base(scheduler == Scheduler.CurrentThread)
        {
            this.source = source;
            this.scheduler = scheduler;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return new ToObservable(this, observer, cancel).Run();
        }

        class ToObservable : OperatorObserverBase<T, T>
        {
            readonly ToObservableObservable<T> parent;

            public ToObservable(ToObservableObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                var e = default(IEnumerator<T>);
                try
                {
                    e = this.parent.source.GetEnumerator();
                }
                catch (Exception exception)
                {
                    this.OnError(exception);
                    return Disposable.Empty;
                }

                if (this.parent.scheduler == Scheduler.Immediate)
                {
                    while (true)
                    {
                        bool hasNext;
                        var current = default(T);
                        try
                        {
                            hasNext = e.MoveNext();
                            if (hasNext) current = e.Current;
                        }
                        catch (Exception ex)
                        {
                            e.Dispose();
                            try {
                                this.observer.OnError(ex); }
                            finally {
                                this.Dispose(); }
                            break;
                        }

                        if (hasNext)
                        {
                            this.observer.OnNext(current);
                        }
                        else
                        {
                            e.Dispose();
                            try {
                                this.observer.OnCompleted(); }
                            finally {
                                this.Dispose(); }
                            break;
                        }
                    }

                    return Disposable.Empty;
                }

                var flag = new SingleAssignmentDisposable();
                flag.Disposable = this.parent.scheduler.Schedule(self =>
                {
                    if (flag.IsDisposed)
                    {
                        e.Dispose();
                        return;
                    }

                    bool hasNext;
                    var current = default(T);
                    try
                    {
                        hasNext = e.MoveNext();
                        if (hasNext) current = e.Current;
                    }
                    catch (Exception ex)
                    {
                        e.Dispose();
                        try {
                            this.observer.OnError(ex); }
                        finally {
                            this.Dispose(); }
                        return;
                    }

                    if (hasNext)
                    {
                        this.observer.OnNext(current);
                        self();
                    }
                    else
                    {
                        e.Dispose();
                        try {
                            this.observer.OnCompleted(); }
                        finally {
                            this.Dispose(); }
                    }
                });

                return flag;
            }

            public override void OnNext(T value)
            {
                // do nothing
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
                try {
                    this.observer.OnCompleted(); }
                finally {
                    this.Dispose(); }
            }
        }
    }
}