using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Schedulers;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class TimerObservable : OperatorObservableBase<long>
    {
        readonly DateTimeOffset? dueTimeA;
        readonly TimeSpan? dueTimeB;
        readonly TimeSpan? period;
        readonly IScheduler scheduler;

        public TimerObservable(DateTimeOffset dueTime, TimeSpan? period, IScheduler scheduler)
            : base(scheduler == Scheduler.CurrentThread)
        {
            this.dueTimeA = dueTime;
            this.period = period;
            this.scheduler = scheduler;
        }

        public TimerObservable(TimeSpan dueTime, TimeSpan? period, IScheduler scheduler)
            : base(scheduler == Scheduler.CurrentThread)
        {
            this.dueTimeB = dueTime;
            this.period = period;
            this.scheduler = scheduler;
        }

        protected override IDisposable SubscribeCore(IObserver<long> observer, IDisposable cancel)
        {
            var timerObserver = new Timer(observer, cancel);

            var dueTime = (this.dueTimeA != null)
                ? this.dueTimeA.Value - this.scheduler.Now
                : this.dueTimeB.Value;

            // one-shot
            if (this.period == null)
            {
                return this.scheduler.Schedule(Scheduler.Normalize(dueTime), () =>
                {
                    timerObserver.OnNext();
                    timerObserver.OnCompleted();
                });
            }
            else
            {
                var periodicScheduler = this.scheduler as ISchedulerPeriodic;
                if (periodicScheduler != null)
                {
                    if (dueTime == this.period.Value)
                    {
                        // same(Observable.Interval), run periodic
                        return periodicScheduler.SchedulePeriodic(Scheduler.Normalize(dueTime), timerObserver.OnNext);
                    }
                    else
                    {
                        // Schedule Once + Scheudle Periodic
                        var disposable = new SerialDisposable();

                        disposable.Disposable = this.scheduler.Schedule(Scheduler.Normalize(dueTime), () =>
                        {
                            timerObserver.OnNext(); // run first

                            var timeP = Scheduler.Normalize(this.period.Value);
                            disposable.Disposable = periodicScheduler.SchedulePeriodic(timeP, timerObserver.OnNext); // run periodic
                        });

                        return disposable;
                    }
                }
                else
                {
                    var timeP = Scheduler.Normalize(this.period.Value);

                    return this.scheduler.Schedule(Scheduler.Normalize(dueTime), self =>
                    {
                        timerObserver.OnNext();
                        self(timeP);
                    });
                }
            }
        }

        class Timer : OperatorObserverBase<long, long>
        {
            long index = 0;

            public Timer(IObserver<long> observer, IDisposable cancel)
                : base(observer, cancel)
            {
            }

            public void OnNext()
            {
                try
                {
                    this.observer.OnNext(this.index++);
                }
                catch
                {
                    this.Dispose();
                    throw;
                }
            }

            public override void OnNext(long value)
            {
                // no use.
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