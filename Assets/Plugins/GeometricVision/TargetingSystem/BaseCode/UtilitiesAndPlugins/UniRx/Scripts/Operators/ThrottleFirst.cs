using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Schedulers;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class ThrottleFirstObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly TimeSpan dueTime;
        readonly IScheduler scheduler;

        public ThrottleFirstObservable(IObservable<T> source, TimeSpan dueTime, IScheduler scheduler) 
            : base(scheduler == Scheduler.CurrentThread || source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.dueTime = dueTime;
            this.scheduler = scheduler;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return new ThrottleFirst(this, observer, cancel).Run();
        }

        class ThrottleFirst : OperatorObserverBase<T, T>
        {
            readonly ThrottleFirstObservable<T> parent;
            readonly object gate = new object();
            bool open = true;
            SerialDisposable cancelable;

            public ThrottleFirst(ThrottleFirstObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.cancelable = new SerialDisposable();
                var subscription = this.parent.source.Subscribe(this);

                return StableCompositeDisposable.Create(this.cancelable, subscription);
            }

            void OnNext()
            {
                lock (this.gate)
                {
                    this.open = true;
                }
            }

            public override void OnNext(T value)
            {
                lock (this.gate)
                {
                    if (!this.open) return;
                    this.observer.OnNext(value);
                    this.open = false;
                }

                var d = new SingleAssignmentDisposable();
                this.cancelable.Disposable = d;
                d.Disposable = this.parent.scheduler.Schedule(this.parent.dueTime, this.OnNext);
            }

            public override void OnError(Exception error)
            {
                this.cancelable.Dispose();

                lock (this.gate)
                {
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
                    try {
                        this.observer.OnCompleted(); } finally {
                        this.Dispose(); }
                }
            }
        }
    }
}