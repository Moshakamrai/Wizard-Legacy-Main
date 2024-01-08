using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Schedulers;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class TimestampObservable<T> : OperatorObservableBase<Timestamped<T>>
    {
        readonly IObservable<T> source;
        readonly IScheduler scheduler;

        public TimestampObservable(IObservable<T> source, IScheduler scheduler)
            : base(scheduler == Scheduler.CurrentThread || source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.scheduler = scheduler;
        }

        protected override IDisposable SubscribeCore(IObserver<Timestamped<T>> observer, IDisposable cancel)
        {
            return this.source.Subscribe(new Timestamp(this, observer, cancel));
        }

        class Timestamp : OperatorObserverBase<T, Timestamped<T>>
        {
            readonly TimestampObservable<T> parent;

            public Timestamp(TimestampObservable<T> parent, IObserver<Timestamped<T>> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                this.parent = parent;
            }

            public override void OnNext(T value)
            {
                this.observer.OnNext(new Timestamped<T>(value, this.parent.scheduler.Now));
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