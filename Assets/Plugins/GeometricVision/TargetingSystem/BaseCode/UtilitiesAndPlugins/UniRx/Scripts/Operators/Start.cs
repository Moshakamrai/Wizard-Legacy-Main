using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Schedulers;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class StartObservable<T> : OperatorObservableBase<T>
    {
        readonly Action action;
        readonly Func<T> function;
        readonly IScheduler scheduler;
        readonly TimeSpan? startAfter;

        public StartObservable(Func<T> function, TimeSpan? startAfter, IScheduler scheduler)
            : base(scheduler == Scheduler.CurrentThread)
        {
            this.function = function;
            this.startAfter = startAfter;
            this.scheduler = scheduler;
        }

        public StartObservable(Action action, TimeSpan? startAfter, IScheduler scheduler)
            : base(scheduler == Scheduler.CurrentThread)
        {
            this.action = action;
            this.startAfter = startAfter;
            this.scheduler = scheduler;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            if (this.startAfter != null)
            {
                return this.scheduler.Schedule(this.startAfter.Value, new StartObserver(this, observer, cancel).Run);
            }
            else
            {
                return this.scheduler.Schedule(new StartObserver(this, observer, cancel).Run);
            }
        }

        class StartObserver : OperatorObserverBase<T, T>
        {
            readonly StartObservable<T> parent;

            public StartObserver(StartObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public void Run()
            {
                var result = default(T);
                try
                {
                    if (this.parent.function != null)
                    {
                        result = this.parent.function();
                    }
                    else
                    {
                        this.parent.action();
                    }
                }
                catch (Exception exception)
                {
                    try {
                        this.observer.OnError(exception); }
                    finally {
                        this.Dispose(); }
                    return;
                }

                this.OnNext(result);
                try {
                    this.observer.OnCompleted(); }
                finally {
                    this.Dispose(); }
            }

            public override void OnNext(T value)
            {
                try
                {
                    this.observer.OnNext(value);
                }
                catch
                {
                    this.Dispose();
                    throw;
                }
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