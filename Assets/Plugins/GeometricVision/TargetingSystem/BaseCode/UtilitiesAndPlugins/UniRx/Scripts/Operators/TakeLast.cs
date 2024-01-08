using System;
using System.Collections.Generic;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Schedulers;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class TakeLastObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;

        // count
        readonly int count;

        // duration
        readonly TimeSpan duration;
        readonly IScheduler scheduler;

        public TakeLastObservable(IObservable<T> source, int count)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.count = count;
        }

        public TakeLastObservable(IObservable<T> source, TimeSpan duration, IScheduler scheduler)
            : base(scheduler == Scheduler.CurrentThread || source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.duration = duration;
            this.scheduler = scheduler;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            if (this.scheduler == null)
            {
                return new TakeLast(this, observer, cancel).Run();
            }
            else
            {
                return new TakeLast_(this, observer, cancel).Run();
            }
        }

        // count
        class TakeLast : OperatorObserverBase<T, T>
        {
            readonly TakeLastObservable<T> parent;
            readonly Queue<T> q;

            public TakeLast(TakeLastObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
                this.q = new Queue<T>();
            }

            public IDisposable Run()
            {
                return this.parent.source.Subscribe(this);
            }

            public override void OnNext(T value)
            {
                this.q.Enqueue(value);
                if (this.q.Count > this.parent.count)
                {
                    this.q.Dequeue();
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
                foreach (var item in this.q)
                {
                    this.observer.OnNext(item);
                }
                try {
                    this.observer.OnCompleted(); } finally {
                    this.Dispose(); }
            }
        }

        // time
        class TakeLast_ : OperatorObserverBase<T, T>
        {
            DateTimeOffset startTime;
            readonly TakeLastObservable<T> parent;
            readonly Queue<TimeInterval<T>> q;

            public TakeLast_(TakeLastObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
                this.q = new Queue<TimeInterval<T>>();
            }

            public IDisposable Run()
            {
                this.startTime = this.parent.scheduler.Now;
                return this.parent.source.Subscribe(this);
            }

            public override void OnNext(T value)
            {
                var now = this.parent.scheduler.Now;
                var elapsed = now - this.startTime;
                this.q.Enqueue(new TimeInterval<T>(value, elapsed));
                this.Trim(elapsed);
            }

            public override void OnError(Exception error)
            {
                try {
                    this.observer.OnError(error); } finally {
                    this.Dispose(); };
            }

            public override void OnCompleted()
            {
                var now = this.parent.scheduler.Now;
                var elapsed = now - this.startTime;
                this.Trim(elapsed);

                foreach (var item in this.q)
                {
                    this.observer.OnNext(item.Value);
                }
                try {
                    this.observer.OnCompleted(); } finally {
                    this.Dispose(); };
            }

            void Trim(TimeSpan now)
            {
                while (this.q.Count > 0 && now - this.q.Peek().Interval >= this.parent.duration)
                {
                    this.q.Dequeue();
                }
            }
        }
    }
}