using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Schedulers;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class TakeObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly int count;
        readonly TimeSpan duration;
        internal readonly IScheduler scheduler; // public for optimization check

        public TakeObservable(IObservable<T> source, int count)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.count = count;
        }

        public TakeObservable(IObservable<T> source, TimeSpan duration, IScheduler scheduler)
            : base(scheduler == Scheduler.CurrentThread || source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.duration = duration;
            this.scheduler = scheduler;
        }

        // optimize combiner

        public IObservable<T> Combine(int count)
        {
            // xs = 6
            // xs.Take(5) = 5         | xs.Take(3) = 3
            // xs.Take(5).Take(3) = 3 | xs.Take(3).Take(5) = 3

            // use minimum one
            return (this.count <= count)
                ? this
                : new TakeObservable<T>(this.source, count);
        }

        public IObservable<T> Combine(TimeSpan duration)
        {
            // xs = 6s
            // xs.Take(5s) = 5s          | xs.Take(3s) = 3s
            // xs.Take(5s).Take(3s) = 3s | xs.Take(3s).Take(5s) = 3s

            // use minimum one
            return (this.duration <= duration)
                ? this
                : new TakeObservable<T>(this.source, duration, this.scheduler);
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            if (this.scheduler == null)
            {
                return this.source.Subscribe(new Take(this, observer, cancel));
            }
            else
            {
                return new Take_(this, observer, cancel).Run();
            }
        }

        class Take : OperatorObserverBase<T, T>
        {
            int rest;

            public Take(TakeObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.rest = parent.count;
            }

            public override void OnNext(T value)
            {
                if (this.rest > 0)
                {
                    this.rest -= 1;
                    this.observer.OnNext(value);
                    if (this.rest == 0)
                    {
                        try {
                            this.observer.OnCompleted(); } finally {
                            this.Dispose(); };
                    }
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

        class Take_ : OperatorObserverBase<T, T>
        {
            readonly TakeObservable<T> parent;
            readonly object gate = new object();

            public Take_(TakeObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
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
                lock (this.gate)
                {
                    try {
                        this.observer.OnCompleted(); } finally {
                        this.Dispose(); };
                }
            }

            public override void OnNext(T value)
            {
                lock (this.gate)
                {
                    this.observer.OnNext(value);
                }
            }

            public override void OnError(Exception error)
            {
                lock (this.gate)
                {
                    try {
                        this.observer.OnError(error); } finally {
                        this.Dispose(); };
                }
            }

            public override void OnCompleted()
            {
                lock (this.gate)
                {
                    try {
                        this.observer.OnCompleted(); } finally {
                        this.Dispose(); };
                }
            }
        }
    }
}