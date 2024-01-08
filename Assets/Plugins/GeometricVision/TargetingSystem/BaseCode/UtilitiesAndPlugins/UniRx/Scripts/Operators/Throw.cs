using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Schedulers;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class ThrowObservable<T> : OperatorObservableBase<T>
    {
        readonly Exception error;
        readonly IScheduler scheduler;

        public ThrowObservable(Exception error, IScheduler scheduler)
            : base(scheduler == Scheduler.CurrentThread)
        {
            this.error = error;
            this.scheduler = scheduler;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            observer = new Throw(observer, cancel);

            if (this.scheduler == Scheduler.Immediate)
            {
                observer.OnError(this.error);
                return Disposable.Empty;
            }
            else
            {
                return this.scheduler.Schedule(() =>
                {
                    observer.OnError(this.error);
                    observer.OnCompleted();
                });
            }
        }

        class Throw : OperatorObserverBase<T, T>
        {
            public Throw(IObserver<T> observer, IDisposable cancel)
                : base(observer, cancel)
            {
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
