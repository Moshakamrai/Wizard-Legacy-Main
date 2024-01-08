using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Schedulers;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class SampleObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly TimeSpan interval;
        readonly IScheduler scheduler;

        public SampleObservable(IObservable<T> source, TimeSpan interval, IScheduler scheduler)
            : base(source.IsRequiredSubscribeOnCurrentThread() || scheduler == Scheduler.CurrentThread)
        {
            this.source = source;
            this.interval = interval;
            this.scheduler = scheduler;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return new Sample(this, observer, cancel).Run();
        }

        class Sample : OperatorObserverBase<T, T>
        {
            readonly SampleObservable<T> parent;
            readonly object gate = new object();
            T latestValue = default(T);
            bool isUpdated = false;
            bool isCompleted = false;
            SingleAssignmentDisposable sourceSubscription;

            public Sample(SampleObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.sourceSubscription = new SingleAssignmentDisposable();
                this.sourceSubscription.Disposable = this.parent.source.Subscribe(this);


                IDisposable scheduling;
                var periodicScheduler = this.parent.scheduler as ISchedulerPeriodic;
                if (periodicScheduler != null)
                {
                    scheduling = periodicScheduler.SchedulePeriodic(this.parent.interval, this.OnNextTick);
                }
                else
                {
                    scheduling = this.parent.scheduler.Schedule(this.parent.interval, this.OnNextRecursive);
                }

                return StableCompositeDisposable.Create(this.sourceSubscription, scheduling);
            }

            void OnNextTick()
            {
                lock (this.gate)
                {
                    if (this.isUpdated)
                    {
                        var value = this.latestValue;
                        this.isUpdated = false;
                        this.observer.OnNext(value);
                    }
                    if (this.isCompleted)
                    {
                        try {
                            this.observer.OnCompleted(); } finally {
                            this.Dispose(); }
                    }
                }
            }

            void OnNextRecursive(Action<TimeSpan> self)
            {
                lock (this.gate)
                {
                    if (this.isUpdated)
                    {
                        var value = this.latestValue;
                        this.isUpdated = false;
                        this.observer.OnNext(value);
                    }
                    if (this.isCompleted)
                    {
                        try {
                            this.observer.OnCompleted(); } finally {
                            this.Dispose(); }
                    }
                }
                self(this.parent.interval);
            }

            public override void OnNext(T value)
            {
                lock (this.gate)
                {
                    this.latestValue = value;
                    this.isUpdated = true;
                }
            }

            public override void OnError(Exception error)
            {
                lock (this.gate)
                {
                    try { this.observer.OnError(error); } finally {
                        this.Dispose(); }
                }
            }

            public override void OnCompleted()
            {
                lock (this.gate)
                {
                    this.isCompleted = true;
                    this.sourceSubscription.Dispose();
                }
            }
        }
    }

    internal class SampleObservable<T, T2> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly IObservable<T2> intervalSource;

        public SampleObservable(IObservable<T> source, IObservable<T2> intervalSource)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.intervalSource = intervalSource;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return new Sample(this, observer, cancel).Run();
        }

        class Sample : OperatorObserverBase<T, T>
        {
            readonly SampleObservable<T, T2> parent;
            readonly object gate = new object();
            T latestValue = default(T);
            bool isUpdated = false;
            bool isCompleted = false;
            SingleAssignmentDisposable sourceSubscription;

            public Sample(
                SampleObservable<T, T2> parent, IObserver<T> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.sourceSubscription = new SingleAssignmentDisposable();
                this.sourceSubscription.Disposable = this.parent.source.Subscribe(this);

                var scheduling = this.parent.intervalSource.Subscribe(new SampleTick(this));

                return StableCompositeDisposable.Create(this.sourceSubscription, scheduling);
            }

            public override void OnNext(T value)
            {
                lock (this.gate)
                {
                    this.latestValue = value;
                    this.isUpdated = true;
                }
            }

            public override void OnError(Exception error)
            {
                lock (this.gate)
                {
                    try { this.observer.OnError(error); } finally {
                        this.Dispose(); }
                }
            }

            public override void OnCompleted()
            {
                lock (this.gate)
                {
                    this.isCompleted = true;
                    this.sourceSubscription.Dispose();
                }
            }

            class SampleTick : IObserver<T2>
            {
                readonly Sample parent;

                public SampleTick(Sample parent)
                {
                    this.parent = parent;
                }

                public void OnCompleted()
                {
                    lock (this.parent.gate)
                    {
                        if (this.parent.isUpdated)
                        {
                            this.parent.isUpdated = false;
                            this.parent.observer.OnNext(this.parent.latestValue);
                        }
                        if (this.parent.isCompleted)
                        {
                            try {
                                this.parent.observer.OnCompleted(); } finally {
                                this.parent.Dispose(); }
                        }
                    }
                }

                public void OnError(Exception error)
                {
                }

                public void OnNext(T2 _)
                {
                    lock (this.parent.gate)
                    {
                        if (this.parent.isUpdated)
                        {
                            var value = this.parent.latestValue;
                            this.parent.isUpdated = false;
                            this.parent.observer.OnNext(value);
                        }
                        if (this.parent.isCompleted)
                        {
                            try {
                                this.parent.observer.OnCompleted(); } finally {
                                this.parent.Dispose(); }
                        }
                    }
                }
            }
        }
    }
}