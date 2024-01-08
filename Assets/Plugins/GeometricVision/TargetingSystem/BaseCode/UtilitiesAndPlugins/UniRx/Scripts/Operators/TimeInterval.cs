using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Schedulers;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class TimeIntervalObservable<T> : OperatorObservableBase<TimeInterval<T>>
    {
        readonly IObservable<T> source;
        readonly IScheduler scheduler;

        public TimeIntervalObservable(IObservable<T> source, IScheduler scheduler)
            : base(scheduler == Scheduler.CurrentThread || source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.scheduler = scheduler;
        }

        protected override IDisposable SubscribeCore(IObserver<TimeInterval<T>> observer, IDisposable cancel)
        {
            return this.source.Subscribe(new TimeInterval(this, observer, cancel));
        }

        class TimeInterval : OperatorObserverBase<T, TimeInterval<T>>
        {
            readonly TimeIntervalObservable<T> parent;
            DateTimeOffset lastTime;

            public TimeInterval(TimeIntervalObservable<T> parent, IObserver<TimeInterval<T>> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                this.parent = parent;
                this.lastTime = parent.scheduler.Now;
            }

            public override void OnNext(T value)
            {
                var now = this.parent.scheduler.Now;
                var span = now.Subtract(this.lastTime);
                this.lastTime = now;

                this.observer.OnNext(new TimeInterval<T>(value, span));
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