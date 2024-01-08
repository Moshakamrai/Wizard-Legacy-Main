using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Schedulers;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class TimeoutObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly TimeSpan? dueTime;
        readonly DateTimeOffset? dueTimeDT;
        readonly IScheduler scheduler;

        public TimeoutObservable(IObservable<T> source, TimeSpan dueTime, IScheduler scheduler) 
            : base(scheduler == Scheduler.CurrentThread || source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.dueTime = dueTime;
            this.scheduler = scheduler;
        }

        public TimeoutObservable(IObservable<T> source, DateTimeOffset dueTime, IScheduler scheduler) 
            : base(scheduler == Scheduler.CurrentThread || source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.dueTimeDT = dueTime;
            this.scheduler = scheduler;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            if (this.dueTime != null)
            {
                return new Timeout(this, observer, cancel).Run();
            }
            else
            {
                return new Timeout_(this, observer, cancel).Run();
            }
        }

        class Timeout : OperatorObserverBase<T, T>
        {
            readonly TimeoutObservable<T> parent;
            readonly object gate = new object();
            ulong objectId = 0ul;
            bool isTimeout = false;
            SingleAssignmentDisposable sourceSubscription;
            SerialDisposable timerSubscription;

            public Timeout(TimeoutObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.sourceSubscription = new SingleAssignmentDisposable();
                this.timerSubscription = new SerialDisposable();
                this.timerSubscription.Disposable = this.RunTimer(this.objectId);
                this.sourceSubscription.Disposable = this.parent.source.Subscribe(this);

                return StableCompositeDisposable.Create(this.timerSubscription, this.sourceSubscription);
            }

            IDisposable RunTimer(ulong timerId)
            {
                return this.parent.scheduler.Schedule(this.parent.dueTime.Value, () =>
                {
                    lock (this.gate)
                    {
                        if (this.objectId == timerId)
                        {
                            this.isTimeout = true;
                        }
                    }
                    if (this.isTimeout)
                    {
                        try {
                            this.observer.OnError(new TimeoutException()); } finally {
                            this.Dispose(); }
                    }
                });
            }

            public override void OnNext(T value)
            {
                ulong useObjectId;
                bool timeout;
                lock (this.gate)
                {
                    timeout = this.isTimeout;
                    this.objectId++;
                    useObjectId = this.objectId;
                }
                if (timeout) return;

                this.timerSubscription.Disposable = Disposable.Empty; // cancel old timer
                this.observer.OnNext(value);
                this.timerSubscription.Disposable = this.RunTimer(useObjectId);
            }

            public override void OnError(Exception error)
            {
                bool timeout;
                lock (this.gate)
                {
                    timeout = this.isTimeout;
                    this.objectId++;
                }
                if (timeout) return;

                this.timerSubscription.Dispose();
                try {
                    this.observer.OnError(error); } finally {
                    this.Dispose(); }
            }

            public override void OnCompleted()
            {
                bool timeout;
                lock (this.gate)
                {
                    timeout = this.isTimeout;
                    this.objectId++;
                }
                if (timeout) return;

                this.timerSubscription.Dispose();
                try {
                    this.observer.OnCompleted(); } finally {
                    this.Dispose(); }
            }
        }

        class Timeout_ : OperatorObserverBase<T, T>
        {
            readonly TimeoutObservable<T> parent;
            readonly object gate = new object();
            bool isFinished = false;
            SingleAssignmentDisposable sourceSubscription;
            IDisposable timerSubscription;

            public Timeout_(TimeoutObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.sourceSubscription = new SingleAssignmentDisposable();

                this.timerSubscription = this.parent.scheduler.Schedule(this.parent.dueTimeDT.Value, this.OnNext);
                this.sourceSubscription.Disposable = this.parent.source.Subscribe(this);

                return StableCompositeDisposable.Create(this.timerSubscription, this.sourceSubscription);
            }

            // in timer
            void OnNext()
            {
                lock (this.gate)
                {
                    if (this.isFinished) return;
                    this.isFinished = true;
                }

                this.sourceSubscription.Dispose();
                try {
                    this.observer.OnError(new TimeoutException()); } finally {
                    this.Dispose(); }
            }

            public override void OnNext(T value)
            {
                lock (this.gate)
                {
                    if (!this.isFinished) this.observer.OnNext(value);
                }
            }

            public override void OnError(Exception error)
            {
                lock (this.gate)
                {
                    if (this.isFinished) return;
                    this.isFinished = true;
                    this.timerSubscription.Dispose();
                }
                try {
                    this.observer.OnError(error); } finally {
                    this.Dispose(); }
            }

            public override void OnCompleted()
            {

                lock (this.gate)
                {
                    if (!this.isFinished)
                    {
                        this.isFinished = true;
                        this.timerSubscription.Dispose();
                    }
                    try {
                        this.observer.OnCompleted(); } finally {
                        this.Dispose(); }
                }
            }
        }
    }
}