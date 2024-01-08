using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Schedulers;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class RangeObservable : OperatorObservableBase<int>
    {
        readonly int start;
        readonly int count;
        readonly IScheduler scheduler;

        public RangeObservable(int start, int count, IScheduler scheduler)
            : base(scheduler == Scheduler.CurrentThread)
        {
            if (count < 0) throw new ArgumentOutOfRangeException("count < 0");

            this.start = start;
            this.count = count;
            this.scheduler = scheduler;
        }

        protected override IDisposable SubscribeCore(IObserver<int> observer, IDisposable cancel)
        {
            observer = new Range(observer, cancel);

            if (this.scheduler == Scheduler.Immediate)
            {
                for (int i = 0; i < this.count; i++)
                {
                    int v = this.start + i;
                    observer.OnNext(v);
                }
                observer.OnCompleted();

                return Disposable.Empty;
            }
            else
            {
                var i = 0;
                return this.scheduler.Schedule((Action self) =>
                {
                    if (i < this.count)
                    {
                        int v = this.start + i;
                        observer.OnNext(v);
                        i++;
                        self();
                    }
                    else
                    {
                        observer.OnCompleted();
                    }
                });
            }
        }

        class Range : OperatorObserverBase<int, int>
        {
            public Range(IObserver<int> observer, IDisposable cancel)
                : base(observer, cancel)
            {
            }

            public override void OnNext(int value)
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