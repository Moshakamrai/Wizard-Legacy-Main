using System;
using System.Collections.Generic;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Schedulers;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class DelayObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly TimeSpan dueTime;
        readonly IScheduler scheduler;

        public DelayObservable(IObservable<T> source, TimeSpan dueTime, IScheduler scheduler) 
            : base(scheduler == Scheduler.CurrentThread || source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.dueTime = dueTime;
            this.scheduler = scheduler;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return new Delay(this, observer, cancel).Run();
        }

        class Delay : OperatorObserverBase<T, T>
        {
            readonly DelayObservable<T> parent;
            readonly object gate = new object();
            bool hasFailed;
            bool running;
            bool active;
            Exception exception;
            Queue<Timestamped<T>> queue;
            bool onCompleted;
            DateTimeOffset completeAt;
            IDisposable sourceSubscription;
            TimeSpan delay;
            bool ready;
            SerialDisposable cancelable;

            public Delay(DelayObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.cancelable = new SerialDisposable();

                this.active = false;
                this.running = false;
                this.queue = new Queue<Timestamped<T>>();
                this.onCompleted = false;
                this.completeAt = default(DateTimeOffset);
                this.hasFailed = false;
                this.exception = default(Exception);
                this.ready = true;
                this.delay = Scheduler.Normalize(this.parent.dueTime);

                var _sourceSubscription = new SingleAssignmentDisposable();
                this.sourceSubscription = _sourceSubscription; // assign to field
                _sourceSubscription.Disposable = this.parent.source.Subscribe(this);

                return StableCompositeDisposable.Create(this.sourceSubscription, this.cancelable);
            }

            public override void OnNext(T value)
            {
                var next = this.parent.scheduler.Now.Add(this.delay);
                var shouldRun = false;

                lock (this.gate)
                {
                    this.queue.Enqueue(new Timestamped<T>(value, next));

                    shouldRun = this.ready && !this.active;
                    this.active = true;
                }

                if (shouldRun)
                {
                    this.cancelable.Disposable = this.parent.scheduler.Schedule(this.delay, this.DrainQueue);
                }
            }

            public override void OnError(Exception error)
            {
                this.sourceSubscription.Dispose();

                var shouldRun = false;

                lock (this.gate)
                {
                    this.queue.Clear();

                    this.exception = error;
                    this.hasFailed = true;

                    shouldRun = !this.running;
                }

                if (shouldRun)
                {
                    try { this.observer.OnError(error); } finally {
                        this.Dispose(); }
                }
            }

            public override void OnCompleted()
            {
                this.sourceSubscription.Dispose();

                var next = this.parent.scheduler.Now.Add(this.delay);
                var shouldRun = false;

                lock (this.gate)
                {
                    this.completeAt = next;
                    this.onCompleted = true;

                    shouldRun = this.ready && !this.active;
                    this.active = true;
                }

                if (shouldRun)
                {
                    this.cancelable.Disposable = this.parent.scheduler.Schedule(this.delay, this.DrainQueue);
                }
            }

            void DrainQueue(Action<TimeSpan> recurse)
            {
                lock (this.gate)
                {
                    if (this.hasFailed) return;
                    this.running = true;
                }

                var shouldYield = false;

                while (true)
                {
                    var hasFailed = false;
                    var error = default(Exception);

                    var hasValue = false;
                    var value = default(T);
                    var hasCompleted = false;

                    var shouldRecurse = false;
                    var recurseDueTime = default(TimeSpan);

                    lock (this.gate)
                    {
                        if (hasFailed)
                        {
                            error = this.exception;
                            hasFailed = true;
                            this.running = false;
                        }
                        else
                        {
                            if (this.queue.Count > 0)
                            {
                                var nextDue = this.queue.Peek().Timestamp;

                                if (nextDue.CompareTo(this.parent.scheduler.Now) <= 0 && !shouldYield)
                                {
                                    value = this.queue.Dequeue().Value;
                                    hasValue = true;
                                }
                                else
                                {
                                    shouldRecurse = true;
                                    recurseDueTime = Scheduler.Normalize(nextDue.Subtract(this.parent.scheduler.Now));
                                    this.running = false;
                                }
                            }
                            else if (this.onCompleted)
                            {
                                if (this.completeAt.CompareTo(this.parent.scheduler.Now) <= 0 && !shouldYield)
                                {
                                    hasCompleted = true;
                                }
                                else
                                {
                                    shouldRecurse = true;
                                    recurseDueTime = Scheduler.Normalize(this.completeAt.Subtract(this.parent.scheduler.Now));
                                    this.running = false;
                                }
                            }
                            else
                            {
                                this.running = false;
                                this.active = false;
                            }
                        }
                    }

                    if (hasValue)
                    {
                        this.observer.OnNext(value);
                        shouldYield = true;
                    }
                    else
                    {
                        if (hasCompleted)
                        {
                            try { this.observer.OnCompleted(); } finally {
                                this.Dispose(); }
                        }
                        else if (hasFailed)
                        {
                            try { this.observer.OnError(error); } finally {
                                this.Dispose(); }
                        }
                        else if (shouldRecurse)
                        {
                            recurse(recurseDueTime);
                        }

                        return;
                    }
                }
            }
        }
    }
}