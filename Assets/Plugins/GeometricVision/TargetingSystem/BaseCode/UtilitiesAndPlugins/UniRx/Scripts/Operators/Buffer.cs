using System;
using System.Collections.Generic;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Schedulers;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class BufferObservable<T> : OperatorObservableBase<IList<T>>
    {
        readonly IObservable<T> source;
        readonly int count;
        readonly int skip;

        readonly TimeSpan timeSpan;
        readonly TimeSpan timeShift;
        readonly IScheduler scheduler;

        public BufferObservable(IObservable<T> source, int count, int skip)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.count = count;
            this.skip = skip;
        }

        public BufferObservable(IObservable<T> source, TimeSpan timeSpan, TimeSpan timeShift, IScheduler scheduler)
            : base(scheduler == Scheduler.CurrentThread || source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.timeSpan = timeSpan;
            this.timeShift = timeShift;
            this.scheduler = scheduler;
        }

        public BufferObservable(IObservable<T> source, TimeSpan timeSpan, int count, IScheduler scheduler)
            : base(scheduler == Scheduler.CurrentThread || source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.timeSpan = timeSpan;
            this.count = count;
            this.scheduler = scheduler;
        }

        protected override IDisposable SubscribeCore(IObserver<IList<T>> observer, IDisposable cancel)
        {
            // count,skip
            if (this.scheduler == null)
            {
                if (this.skip == 0)
                {
                    return new Buffer(this, observer, cancel).Run();
                }
                else
                {
                    return new Buffer_(this, observer, cancel).Run();
                }
            }
            else
            {
                // time + count
                if (this.count > 0)
                {
                    return new BufferTC(this, observer, cancel).Run();
                }
                else
                {
                    if (this.timeSpan == this.timeShift)
                    {
                        return new BufferT(this, observer, cancel).Run();
                    }
                    else
                    {
                        return new BufferTS(this, observer, cancel).Run();
                    }
                }
            }
        }

        // count only
        class Buffer : OperatorObserverBase<T, IList<T>>
        {
            readonly BufferObservable<T> parent;
            List<T> list;

            public Buffer(BufferObservable<T> parent, IObserver<IList<T>> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.list = new List<T>(this.parent.count);
                return this.parent.source.Subscribe(this);
            }

            public override void OnNext(T value)
            {
                this.list.Add(value);
                if (this.list.Count == this.parent.count)
                {
                    this.observer.OnNext(this.list);
                    this.list = new List<T>(this.parent.count);
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
                if (this.list.Count > 0)
                {
                    this.observer.OnNext(this.list);
                }
                try {
                    this.observer.OnCompleted(); } finally {
                    this.Dispose(); }
            }
        }

        // count and skip
        class Buffer_ : OperatorObserverBase<T, IList<T>>
        {
            readonly BufferObservable<T> parent;
            Queue<List<T>> q;
            int index;

            public Buffer_(BufferObservable<T> parent, IObserver<IList<T>> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.q = new Queue<List<T>>();
                this.index = -1;
                return this.parent.source.Subscribe(this);
            }

            public override void OnNext(T value)
            {
                this.index++;

                if (this.index % this.parent.skip == 0)
                {
                    this.q.Enqueue(new List<T>(this.parent.count));
                }

                var len = this.q.Count;
                for (int i = 0; i < len; i++)
                {
                    var list = this.q.Dequeue();
                    list.Add(value);
                    if (list.Count == this.parent.count)
                    {
                        this.observer.OnNext(list);
                    }
                    else
                    {
                        this.q.Enqueue(list);
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
                foreach (var list in this.q)
                {
                    this.observer.OnNext(list);
                }
                try {
                    this.observer.OnCompleted(); } finally {
                    this.Dispose(); }
            }
        }

        // timespan = timeshift
        class BufferT : OperatorObserverBase<T, IList<T>>
        {
            static readonly T[] EmptyArray = new T[0];

            readonly BufferObservable<T> parent;
            readonly object gate = new object();

            List<T> list;

            public BufferT(BufferObservable<T> parent, IObserver<IList<T>> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.list = new List<T>();

                var timerSubscription = Observable.Interval(this.parent.timeSpan, this.parent.scheduler)
                    .Subscribe(new Buffer(this));

                var sourceSubscription = this.parent.source.Subscribe(this);

                return StableCompositeDisposable.Create(timerSubscription, sourceSubscription);
            }

            public override void OnNext(T value)
            {
                lock (this.gate)
                {
                    this.list.Add(value);
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
                List<T> currentList;
                lock (this.gate)
                {
                    currentList = this.list;
                }

                this.observer.OnNext(currentList);
                try {
                    this.observer.OnCompleted(); } finally {
                    this.Dispose(); }
            }

            class Buffer : IObserver<long>
            {
                BufferT parent;

                public Buffer(BufferT parent)
                {
                    this.parent = parent;
                }

                public void OnNext(long value)
                {
                    var isZero = false;
                    List<T> currentList;
                    lock (this.parent.gate)
                    {
                        currentList = this.parent.list;
                        if (currentList.Count != 0)
                        {
                            this.parent.list = new List<T>();
                        }
                        else
                        {
                            isZero = true;
                        }
                    }

                    this.parent.observer.OnNext((isZero) ? (IList<T>)EmptyArray : currentList);
                }

                public void OnError(Exception error)
                {
                }

                public void OnCompleted()
                {
                }
            }
        }

        // timespan + timeshift
        class BufferTS : OperatorObserverBase<T, IList<T>>
        {
            readonly BufferObservable<T> parent;
            readonly object gate = new object();

            Queue<IList<T>> q;
            TimeSpan totalTime;
            TimeSpan nextShift;
            TimeSpan nextSpan;
            SerialDisposable timerD;

            public BufferTS(BufferObservable<T> parent, IObserver<IList<T>> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.totalTime = TimeSpan.Zero;
                this.nextShift = this.parent.timeShift;
                this.nextSpan = this.parent.timeSpan;

                this.q = new Queue<IList<T>>();

                this.timerD = new SerialDisposable();
                this.q.Enqueue(new List<T>());
                this.CreateTimer();

                var subscription = this.parent.source.Subscribe(this);

                return StableCompositeDisposable.Create(subscription, this.timerD);
            }

            void CreateTimer()
            {
                var m = new SingleAssignmentDisposable();
                this.timerD.Disposable = m;

                var isSpan = false;
                var isShift = false;
                if (this.nextSpan == this.nextShift)
                {
                    isSpan = true;
                    isShift = true;
                }
                else if (this.nextSpan < this.nextShift)
                    isSpan = true;
                else
                    isShift = true;

                var newTotalTime = isSpan ? this.nextSpan : this.nextShift;
                var ts = newTotalTime - this.totalTime;
                this.totalTime = newTotalTime;

                if (isSpan) this.nextSpan += this.parent.timeShift;
                if (isShift) this.nextShift += this.parent.timeShift;

                m.Disposable = this.parent.scheduler.Schedule(ts, () =>
                {
                    lock (this.gate)
                    {
                        if (isShift)
                        {
                            var s = new List<T>();
                            this.q.Enqueue(s);
                        }
                        if (isSpan)
                        {
                            var s = this.q.Dequeue();
                            this.observer.OnNext(s);
                        }
                    }

                    this.CreateTimer();
                });
            }

            public override void OnNext(T value)
            {
                lock (this.gate)
                {
                    foreach (var s in this.q)
                    {
                        s.Add(value);
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
                lock (this.gate)
                {
                    foreach (var list in this.q)
                    {
                        this.observer.OnNext(list);
                    }

                    try {
                        this.observer.OnCompleted(); } finally {
                        this.Dispose(); }
                }
            }
        }

        // timespan + count
        class BufferTC : OperatorObserverBase<T, IList<T>>
        {
            static readonly T[] EmptyArray = new T[0]; // cache

            readonly BufferObservable<T> parent;
            readonly object gate = new object();

            List<T> list;
            long timerId;
            SerialDisposable timerD;

            public BufferTC(BufferObservable<T> parent, IObserver<IList<T>> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.list = new List<T>();
                this.timerId = 0L;
                this.timerD = new SerialDisposable();

                this.CreateTimer();
                var subscription = this.parent.source.Subscribe(this);

                return StableCompositeDisposable.Create(subscription, this.timerD);
            }

            void CreateTimer()
            {
                var currentTimerId = this.timerId;
                var timerS = new SingleAssignmentDisposable();
                this.timerD.Disposable = timerS; // restart timer(dispose before)


                var periodicScheduler = this.parent.scheduler as ISchedulerPeriodic;
                if (periodicScheduler != null)
                {
                    timerS.Disposable = periodicScheduler.SchedulePeriodic(this.parent.timeSpan, () => this.OnNextTick(currentTimerId));
                }
                else
                {
                    timerS.Disposable = this.parent.scheduler.Schedule(this.parent.timeSpan, self => this.OnNextRecursive(currentTimerId, self));
                }
            }

            void OnNextTick(long currentTimerId)
            {
                var isZero = false;
                List<T> currentList;
                lock (this.gate)
                {
                    if (currentTimerId != this.timerId) return;

                    currentList = this.list;
                    if (currentList.Count != 0)
                    {
                        this.list = new List<T>();
                    }
                    else
                    {
                        isZero = true;
                    }
                }

                this.observer.OnNext((isZero) ? (IList<T>)EmptyArray : currentList);
            }

            void OnNextRecursive(long currentTimerId, Action<TimeSpan> self)
            {
                var isZero = false;
                List<T> currentList;
                lock (this.gate)
                {
                    if (currentTimerId != this.timerId) return;

                    currentList = this.list;
                    if (currentList.Count != 0)
                    {
                        this.list = new List<T>();
                    }
                    else
                    {
                        isZero = true;
                    }
                }

                this.observer.OnNext((isZero) ? (IList<T>)EmptyArray : currentList);
                self(this.parent.timeSpan);
            }

            public override void OnNext(T value)
            {
                List<T> currentList = null;
                lock (this.gate)
                {
                    this.list.Add(value);
                    if (this.list.Count == this.parent.count)
                    {
                        currentList = this.list;
                        this.list = new List<T>();
                        this.timerId++;
                        this.CreateTimer();
                    }
                }
                if (currentList != null)
                {
                    this.observer.OnNext(currentList);
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
                List<T> currentList;
                lock (this.gate)
                {
                    this.timerId++;
                    currentList = this.list;
                }

                this.observer.OnNext(currentList);
                try {
                    this.observer.OnCompleted(); } finally {
                    this.Dispose(); }
            }
        }
    }

    internal class BufferObservable<TSource, TWindowBoundary> : OperatorObservableBase<IList<TSource>>
    {
        readonly IObservable<TSource> source;
        readonly IObservable<TWindowBoundary> windowBoundaries;

        public BufferObservable(IObservable<TSource> source, IObservable<TWindowBoundary> windowBoundaries)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.windowBoundaries = windowBoundaries;
        }

        protected override IDisposable SubscribeCore(IObserver<IList<TSource>> observer, IDisposable cancel)
        {
            return new Buffer(this, observer, cancel).Run();
        }

        class Buffer : OperatorObserverBase<TSource, IList<TSource>>
        {
            static readonly TSource[] EmptyArray = new TSource[0]; // cache

            readonly BufferObservable<TSource, TWindowBoundary> parent;
            object gate = new object();
            List<TSource> list;

            public Buffer(BufferObservable<TSource, TWindowBoundary> parent, IObserver<IList<TSource>> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.list = new List<TSource>();

                var sourceSubscription = this.parent.source.Subscribe(this);
                var windowSubscription = this.parent.windowBoundaries.Subscribe(new Buffer_(this));

                return StableCompositeDisposable.Create(sourceSubscription, windowSubscription);
            }

            public override void OnNext(TSource value)
            {
                lock (this.gate)
                {
                    this.list.Add(value);
                }
            }

            public override void OnError(Exception error)
            {
                lock (this.gate)
                {
                    try {
                        this.observer.OnError(error); } finally {
                        this.Dispose(); }
                }
            }

            public override void OnCompleted()
            {
                lock (this.gate)
                {
                    var currentList = this.list;
                    this.list = new List<TSource>(); // safe
                    this.observer.OnNext(currentList);
                    try {
                        this.observer.OnCompleted(); } finally {
                        this.Dispose(); }
                }
            }

            class Buffer_ : IObserver<TWindowBoundary>
            {
                readonly Buffer parent;

                public Buffer_(Buffer parent)
                {
                    this.parent = parent;
                }

                public void OnNext(TWindowBoundary value)
                {
                    var isZero = false;
                    List<TSource> currentList;
                    lock (this.parent.gate)
                    {
                        currentList = this.parent.list;
                        if (currentList.Count != 0)
                        {
                            this.parent.list = new List<TSource>();
                        }
                        else
                        {
                            isZero = true;
                        }
                    }
                    if (isZero)
                    {
                        this.parent.observer.OnNext(EmptyArray);
                    }
                    else
                    {
                        this.parent.observer.OnNext(currentList);
                    }
                }

                public void OnError(Exception error)
                {
                    this.parent.OnError(error);
                }

                public void OnCompleted()
                {
                    this.parent.OnCompleted();
                }
            }
        }
    }
}