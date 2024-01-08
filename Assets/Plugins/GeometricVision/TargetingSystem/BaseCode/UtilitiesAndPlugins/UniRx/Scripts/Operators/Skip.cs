using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Schedulers;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class SkipObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly int count;
        readonly TimeSpan duration;
        internal readonly IScheduler scheduler; // public for optimization check

        public SkipObservable(IObservable<T> source, int count)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.count = count;
        }

        public SkipObservable(IObservable<T> source, TimeSpan duration, IScheduler scheduler)
            : base(scheduler == Scheduler.CurrentThread || source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.duration = duration;
            this.scheduler = scheduler;
        }

        // optimize combiner

        public IObservable<T> Combine(int count)
        {
            // use sum
            // xs = 6
            // xs.Skip(2) = 4
            // xs.Skip(2).Skip(3) = 1

            return new SkipObservable<T>(this.source, this.count + count);
        }

        public IObservable<T> Combine(TimeSpan duration)
        {
            // use max
            // xs = 6s
            // xs.Skip(2s) = 2s
            // xs.Skip(2s).Skip(3s) = 3s

            return (duration <= this.duration)
                ? this
                : new SkipObservable<T>(this.source, duration, this.scheduler);
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            if (this.scheduler == null)
            {
                return this.source.Subscribe(new Skip(this, observer, cancel));
            }
            else
            {
                return new Skip_(this, observer, cancel).Run();
            }
        }

        class Skip : OperatorObserverBase<T, T>
        {
            int remaining;

            public Skip(SkipObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.remaining = parent.count;
            }

            public override void OnNext(T value)
            {
                if (this.remaining <= 0)
                {
                    this.observer.OnNext(value);
                }
                else
                {
                    this.remaining--;
                }
            }

            public override void OnError(Exception error)
            {
                try {
                    this.observer.OnError(error); } finally {
                    this.Dispose(); }
            }

            public override void OnCompleted()
            {
                try {
                    this.observer.OnCompleted(); } finally {
                    this.Dispose(); }
            }
        }

        class Skip_ : OperatorObserverBase<T, T>
        {
            readonly SkipObservable<T> parent;
            volatile bool open;

            public Skip_(SkipObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                var d1 = this.parent.scheduler.Schedule(this.parent.duration, this.Tick);
                var d2 = this.parent.source.Subscribe(this);

                return StableCompositeDisposable.Create(d1, d2);
            }

            void Tick()
            {
                this.open = true;
            }

            public override void OnNext(T value)
            {
                if (this.open)
                {
                    this.observer.OnNext(value);
                }
            }

            public override void OnError(Exception error)
            {
                try {
                    this.observer.OnError(error); } finally {
                    this.Dispose(); };
            }

            public override void OnCompleted()
            {
                try {
                    this.observer.OnCompleted(); } finally {
                    this.Dispose(); };
            }
        }
    }
}