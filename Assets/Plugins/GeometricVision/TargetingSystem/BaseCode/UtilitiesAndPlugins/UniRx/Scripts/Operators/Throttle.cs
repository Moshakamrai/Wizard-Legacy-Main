using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Schedulers;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class ThrottleObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly TimeSpan dueTime;
        readonly IScheduler scheduler;

        public ThrottleObservable(IObservable<T> source, TimeSpan dueTime, IScheduler scheduler) 
            : base(scheduler == Scheduler.CurrentThread || source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.dueTime = dueTime;
            this.scheduler = scheduler;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return new Throttle(this, observer, cancel).Run();
        }

        class Throttle : OperatorObserverBase<T, T>
        {
            readonly ThrottleObservable<T> parent;
            readonly object gate = new object();
            T latestValue = default(T);
            bool hasValue = false;
            SerialDisposable cancelable;
            ulong id = 0;

            public Throttle(ThrottleObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.cancelable = new SerialDisposable();
                var subscription = this.parent.source.Subscribe(this);

                return StableCompositeDisposable.Create(this.cancelable, subscription);
            }

            void OnNext(ulong currentid)
            {
                lock (this.gate)
                {
                    if (this.hasValue && this.id == currentid)
                    {
                        this.observer.OnNext(this.latestValue);
                    }

                    this.hasValue = false;
                }
            }

            public override void OnNext(T value)
            {
                ulong currentid;
                lock (this.gate)
                {
                    this.hasValue = true;
                    this.latestValue = value;
                    this.id = unchecked(this.id + 1);
                    currentid = this.id;
                }

                var d = new SingleAssignmentDisposable();
                this.cancelable.Disposable = d;
                d.Disposable = this.parent.scheduler.Schedule(this.parent.dueTime, () => this.OnNext(currentid));
            }

            public override void OnError(Exception error)
            {
                this.cancelable.Dispose();

                lock (this.gate)
                {
                    this.hasValue = false;
                    this.id = unchecked(this.id + 1);
                    try {
                        this.observer.OnError(error); } finally {
                        this.Dispose(); }
                }
            }

            public override void OnCompleted()
            {
                this.cancelable.Dispose();

                lock (this.gate)
                {
                    if (this.hasValue)
                    {
                        this.observer.OnNext(this.latestValue);
                    }

                    this.hasValue = false;
                    this.id = unchecked(this.id + 1);
                    try {
                        this.observer.OnCompleted(); } finally {
                        this.Dispose(); }
                }
            }
        }
    }
}