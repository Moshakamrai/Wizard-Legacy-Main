using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Schedulers;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class RepeatObservable<T> : OperatorObservableBase<T>
    {
        readonly T value;
        readonly int? repeatCount;
        readonly IScheduler scheduler;

        public RepeatObservable(T value, int? repeatCount, IScheduler scheduler)
            : base(scheduler == Scheduler.CurrentThread)
        {
            this.value = value;
            this.repeatCount = repeatCount;
            this.scheduler = scheduler;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            observer = new Repeat(observer, cancel);

            if (this.repeatCount == null)
            {
                return this.scheduler.Schedule((Action self) =>
                {
                    observer.OnNext(this.value);
                    self();
                });
            }
            else
            {
                if (this.scheduler == Scheduler.Immediate)
                {
                    var count = this.repeatCount.Value;
                    for (int i = 0; i < count; i++)
                    {
                        observer.OnNext(this.value);
                    }
                    observer.OnCompleted();
                    return Disposable.Empty;
                }
                else
                {
                    var currentCount = this.repeatCount.Value;
                    return this.scheduler.Schedule((Action self) =>
                    {
                        if (currentCount > 0)
                        {
                            observer.OnNext(this.value);
                            currentCount--;
                        }

                        if (currentCount == 0)
                        {
                            observer.OnCompleted();
                            return;
                        }

                        self();
                    });
                }
            }
        }

        class Repeat : OperatorObserverBase<T, T>
        {
            public Repeat(IObserver<T> observer, IDisposable cancel)
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