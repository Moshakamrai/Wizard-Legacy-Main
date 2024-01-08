using System;
using System.Collections.Generic;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    public delegate TR ZipFunc<T1, T2, T3, TR>(T1 arg1, T2 arg2, T3 arg3);
    public delegate TR ZipFunc<T1, T2, T3, T4, TR>(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
    public delegate TR ZipFunc<T1, T2, T3, T4, T5, TR>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
    public delegate TR ZipFunc<T1, T2, T3, T4, T5, T6, TR>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
    public delegate TR ZipFunc<T1, T2, T3, T4, T5, T6, T7, TR>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);

    // binary
    internal class ZipObservable<TLeft, TRight, TResult> : OperatorObservableBase<TResult>
    {
        readonly IObservable<TLeft> left;
        readonly IObservable<TRight> right;
        readonly Func<TLeft, TRight, TResult> selector;

        public ZipObservable(IObservable<TLeft> left, IObservable<TRight> right, Func<TLeft, TRight, TResult> selector)
            : base(left.IsRequiredSubscribeOnCurrentThread() || right.IsRequiredSubscribeOnCurrentThread())
        {
            this.left = left;
            this.right = right;
            this.selector = selector;
        }

        protected override IDisposable SubscribeCore(IObserver<TResult> observer, IDisposable cancel)
        {
            return new Zip(this, observer, cancel).Run();
        }

        class Zip : OperatorObserverBase<TResult, TResult>
        {
            readonly ZipObservable<TLeft, TRight, TResult> parent;

            readonly object gate = new object();
            readonly Queue<TLeft> leftQ = new Queue<TLeft>();
            bool leftCompleted = false;
            readonly Queue<TRight> rightQ = new Queue<TRight>();
            bool rightCompleted = false;

            public Zip(ZipObservable<TLeft, TRight, TResult> parent, IObserver<TResult> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                var l = this.parent.left.Subscribe(new LeftZipObserver(this));
                var r = this.parent.right.Subscribe(new RightZipObserver(this));

                return StableCompositeDisposable.Create(l, r, Disposable.Create(() =>
                {
                    lock (this.gate)
                    {
                        this.leftQ.Clear();
                        this.rightQ.Clear();
                    }
                }));
            }

            // dequeue is in the lock
            void Dequeue()
            {
                TLeft lv;
                TRight rv;
                TResult v;

                if (this.leftQ.Count != 0 && this.rightQ.Count != 0)
                {
                    lv = this.leftQ.Dequeue();
                    rv = this.rightQ.Dequeue();
                }
                else if (this.leftCompleted || this.rightCompleted)
                {
                    try {
                        this.observer.OnCompleted(); }
                    finally {
                        this.Dispose(); }
                    return;
                }
                else
                {
                    return;
                }

                try
                {
                    v = this.parent.selector(lv, rv);
                }
                catch (Exception ex)
                {
                    try {
                        this.observer.OnError(ex); }
                    finally {
                        this.Dispose(); }
                    return;
                }

                this.OnNext(v);
            }

            public override void OnNext(TResult value)
            {
                this.observer.OnNext(value);
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

            class LeftZipObserver : IObserver<TLeft>
            {
                readonly Zip parent;

                public LeftZipObserver(Zip parent)
                {
                    this.parent = parent;
                }

                public void OnNext(TLeft value)
                {
                    lock (this.parent.gate)
                    {
                        this.parent.leftQ.Enqueue(value);
                        this.parent.Dequeue();
                    }
                }

                public void OnError(Exception ex)
                {
                    lock (this.parent.gate)
                    {
                        this.parent.OnError(ex);
                    }
                }

                public void OnCompleted()
                {
                    lock (this.parent.gate)
                    {
                        this.parent.leftCompleted = true;
                        if (this.parent.rightCompleted) this.parent.OnCompleted();
                    }
                }
            }

            class RightZipObserver : IObserver<TRight>
            {
                readonly Zip parent;

                public RightZipObserver(Zip parent)
                {
                    this.parent = parent;
                }

                public void OnNext(TRight value)
                {
                    lock (this.parent.gate)
                    {
                        this.parent.rightQ.Enqueue(value);
                        this.parent.Dequeue();
                    }
                }

                public void OnError(Exception ex)
                {
                    lock (this.parent.gate)
                    {
                        this.parent.OnError(ex);
                    }
                }

                public void OnCompleted()
                {
                    lock (this.parent.gate)
                    {
                        this.parent.rightCompleted = true;
                        if (this.parent.leftCompleted) this.parent.OnCompleted();
                    }
                }
            }
        }
    }

    // array
    internal class ZipObservable<T> : OperatorObservableBase<IList<T>>
    {
        readonly IObservable<T>[] sources;

        public ZipObservable(IObservable<T>[] sources)
            : base(true)
        {
            this.sources = sources;
        }

        protected override IDisposable SubscribeCore(IObserver<IList<T>> observer, IDisposable cancel)
        {
            return new Zip(this, observer, cancel).Run();
        }

        class Zip : OperatorObserverBase<IList<T>, IList<T>>
        {
            readonly ZipObservable<T> parent;
            readonly object gate = new object();

            Queue<T>[] queues;
            bool[] isDone;
            int length;

            public Zip(ZipObservable<T> parent, IObserver<IList<T>> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.length = this.parent.sources.Length;
                this.queues = new Queue<T>[this.length];
                this.isDone = new bool[this.length];

                for (int i = 0; i < this.length; i++)
                {
                    this.queues[i] = new Queue<T>();
                }

                var disposables = new IDisposable[this.length + 1];
                for (int i = 0; i < this.length; i++)
                {
                    var source = this.parent.sources[i];
                    disposables[i] = source.Subscribe(new ZipObserver(this, i));
                }

                disposables[this.length] = Disposable.Create(() =>
                {
                    lock (this.gate)
                    {
                        for (int i = 0; i < this.length; i++)
                        {
                            var q = this.queues[i];
                            q.Clear();
                        }
                    }
                });

                return StableCompositeDisposable.CreateUnsafe(disposables);
            }

            // dequeue is in the lock
            void Dequeue(int index)
            {
                var allQueueHasValue = true;
                for (int i = 0; i < this.length; i++)
                {
                    if (this.queues[i].Count == 0)
                    {
                        allQueueHasValue = false;
                        break;
                    }
                }

                if (!allQueueHasValue)
                {
                    var allCompletedWithoutSelf = true;
                    for (int i = 0; i < this.length; i++)
                    {
                        if (i == index) continue;
                        if (!this.isDone[i])
                        {
                            allCompletedWithoutSelf = false;
                            break;
                        }
                    }

                    if (allCompletedWithoutSelf)
                    {
                        try {
                            this.observer.OnCompleted(); }
                        finally {
                            this.Dispose(); }
                        return;
                    }
                    else
                    {
                        return;
                    }
                }

                var array = new T[this.length];
                for (int i = 0; i < this.length; i++)
                {
                    array[i] = this.queues[i].Dequeue();
                }

                this.OnNext(array);
            }

            public override void OnNext(IList<T> value)
            {
                this.observer.OnNext(value);
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

            class ZipObserver : IObserver<T>
            {
                readonly Zip parent;
                readonly int index;

                public ZipObserver(Zip parent, int index)
                {
                    this.parent = parent;
                    this.index = index;
                }

                public void OnNext(T value)
                {
                    lock (this.parent.gate)
                    {
                        this.parent.queues[this.index].Enqueue(value);
                        this.parent.Dequeue(this.index);
                    }
                }

                public void OnError(Exception ex)
                {
                    lock (this.parent.gate)
                    {
                        this.parent.OnError(ex);
                    }
                }

                public void OnCompleted()
                {
                    lock (this.parent.gate)
                    {
                        this.parent.isDone[this.index] = true;
                        var allTrue = true;
                        for (int i = 0; i < this.parent.length; i++)
                        {
                            if (!this.parent.isDone[i])
                            {
                                allTrue = false;
                                break;
                            }
                        }

                        if (allTrue)
                        {
                            this.parent.OnCompleted();
                        }
                    }
                }
            }
        }
    }

    // Generated from UniRx.Console.ZipGenerator.tt
    #region NTH

    internal class ZipObservable<T1, T2, T3, TR> : OperatorObservableBase<TR>
    {
        IObservable<T1> source1;
        IObservable<T2> source2;
        IObservable<T3> source3;
        ZipFunc<T1, T2, T3, TR> resultSelector;

        public ZipObservable(
            IObservable<T1> source1,
            IObservable<T2> source2,
            IObservable<T3> source3,
              ZipFunc<T1, T2, T3, TR> resultSelector)
            : base(
                source1.IsRequiredSubscribeOnCurrentThread() ||
                source2.IsRequiredSubscribeOnCurrentThread() ||
                source3.IsRequiredSubscribeOnCurrentThread() ||
                false)
        {
            this.source1 = source1;
            this.source2 = source2;
            this.source3 = source3;
            this.resultSelector = resultSelector;
        }

        protected override IDisposable SubscribeCore(IObserver<TR> observer, IDisposable cancel)
        {
            return new Zip(this, observer, cancel).Run();
        }

        class Zip : NthZipObserverBase<TR>
        {
            readonly ZipObservable<T1, T2, T3, TR> parent;
            readonly object gate = new object();
            readonly Queue<T1> q1 = new Queue<T1>();
            readonly Queue<T2> q2 = new Queue<T2>();
            readonly Queue<T3> q3 = new Queue<T3>();

            public Zip(ZipObservable<T1, T2, T3, TR> parent, IObserver<TR> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.SetQueue(new global::System.Collections.ICollection[] {this.q1, this.q2, this.q3 });
                var s1 = this.parent.source1.Subscribe(new ZipObserver<T1>(this.gate, this, 0, this.q1));
                var s2 = this.parent.source2.Subscribe(new ZipObserver<T2>(this.gate, this, 1, this.q2));
                var s3 = this.parent.source3.Subscribe(new ZipObserver<T3>(this.gate, this, 2, this.q3));

                return StableCompositeDisposable.Create(s1, s2, s3, Disposable.Create(() =>
                {
                    lock (this.gate)
                    {
                        this.q1.Clear();
                        this.q2.Clear();
                        this.q3.Clear();
                    }
                }));
            }

            public override TR GetResult()
            {
                return this.parent.resultSelector(this.q1.Dequeue(), this.q2.Dequeue(), this.q3.Dequeue());
            }

            public override void OnNext(TR value)
            {
                this.observer.OnNext(value);
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


    internal class ZipObservable<T1, T2, T3, T4, TR> : OperatorObservableBase<TR>
    {
        IObservable<T1> source1;
        IObservable<T2> source2;
        IObservable<T3> source3;
        IObservable<T4> source4;
        ZipFunc<T1, T2, T3, T4, TR> resultSelector;

        public ZipObservable(
            IObservable<T1> source1,
            IObservable<T2> source2,
            IObservable<T3> source3,
            IObservable<T4> source4,
              ZipFunc<T1, T2, T3, T4, TR> resultSelector)
            : base(
                source1.IsRequiredSubscribeOnCurrentThread() ||
                source2.IsRequiredSubscribeOnCurrentThread() ||
                source3.IsRequiredSubscribeOnCurrentThread() ||
                source4.IsRequiredSubscribeOnCurrentThread() ||
                false)
        {
            this.source1 = source1;
            this.source2 = source2;
            this.source3 = source3;
            this.source4 = source4;
            this.resultSelector = resultSelector;
        }

        protected override IDisposable SubscribeCore(IObserver<TR> observer, IDisposable cancel)
        {
            return new Zip(this, observer, cancel).Run();
        }

        class Zip : NthZipObserverBase<TR>
        {
            readonly ZipObservable<T1, T2, T3, T4, TR> parent;
            readonly object gate = new object();
            readonly Queue<T1> q1 = new Queue<T1>();
            readonly Queue<T2> q2 = new Queue<T2>();
            readonly Queue<T3> q3 = new Queue<T3>();
            readonly Queue<T4> q4 = new Queue<T4>();

            public Zip(ZipObservable<T1, T2, T3, T4, TR> parent, IObserver<TR> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.SetQueue(new global::System.Collections.ICollection[] {this.q1, this.q2, this.q3, this.q4 });
                var s1 = this.parent.source1.Subscribe(new ZipObserver<T1>(this.gate, this, 0, this.q1));
                var s2 = this.parent.source2.Subscribe(new ZipObserver<T2>(this.gate, this, 1, this.q2));
                var s3 = this.parent.source3.Subscribe(new ZipObserver<T3>(this.gate, this, 2, this.q3));
                var s4 = this.parent.source4.Subscribe(new ZipObserver<T4>(this.gate, this, 3, this.q4));

                return StableCompositeDisposable.Create(s1, s2, s3, s4, Disposable.Create(() =>
                {
                    lock (this.gate)
                    {
                        this.q1.Clear();
                        this.q2.Clear();
                        this.q3.Clear();
                        this.q4.Clear();
                    }
                }));
            }

            public override TR GetResult()
            {
                return this.parent.resultSelector(this.q1.Dequeue(), this.q2.Dequeue(), this.q3.Dequeue(), this.q4.Dequeue());
            }

            public override void OnNext(TR value)
            {
                this.observer.OnNext(value);
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


    internal class ZipObservable<T1, T2, T3, T4, T5, TR> : OperatorObservableBase<TR>
    {
        IObservable<T1> source1;
        IObservable<T2> source2;
        IObservable<T3> source3;
        IObservable<T4> source4;
        IObservable<T5> source5;
        ZipFunc<T1, T2, T3, T4, T5, TR> resultSelector;

        public ZipObservable(
            IObservable<T1> source1,
            IObservable<T2> source2,
            IObservable<T3> source3,
            IObservable<T4> source4,
            IObservable<T5> source5,
              ZipFunc<T1, T2, T3, T4, T5, TR> resultSelector)
            : base(
                source1.IsRequiredSubscribeOnCurrentThread() ||
                source2.IsRequiredSubscribeOnCurrentThread() ||
                source3.IsRequiredSubscribeOnCurrentThread() ||
                source4.IsRequiredSubscribeOnCurrentThread() ||
                source5.IsRequiredSubscribeOnCurrentThread() ||
                false)
        {
            this.source1 = source1;
            this.source2 = source2;
            this.source3 = source3;
            this.source4 = source4;
            this.source5 = source5;
            this.resultSelector = resultSelector;
        }

        protected override IDisposable SubscribeCore(IObserver<TR> observer, IDisposable cancel)
        {
            return new Zip(this, observer, cancel).Run();
        }

        class Zip : NthZipObserverBase<TR>
        {
            readonly ZipObservable<T1, T2, T3, T4, T5, TR> parent;
            readonly object gate = new object();
            readonly Queue<T1> q1 = new Queue<T1>();
            readonly Queue<T2> q2 = new Queue<T2>();
            readonly Queue<T3> q3 = new Queue<T3>();
            readonly Queue<T4> q4 = new Queue<T4>();
            readonly Queue<T5> q5 = new Queue<T5>();

            public Zip(ZipObservable<T1, T2, T3, T4, T5, TR> parent, IObserver<TR> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.SetQueue(new global::System.Collections.ICollection[] {this.q1, this.q2, this.q3, this.q4, this.q5 });
                var s1 = this.parent.source1.Subscribe(new ZipObserver<T1>(this.gate, this, 0, this.q1));
                var s2 = this.parent.source2.Subscribe(new ZipObserver<T2>(this.gate, this, 1, this.q2));
                var s3 = this.parent.source3.Subscribe(new ZipObserver<T3>(this.gate, this, 2, this.q3));
                var s4 = this.parent.source4.Subscribe(new ZipObserver<T4>(this.gate, this, 3, this.q4));
                var s5 = this.parent.source5.Subscribe(new ZipObserver<T5>(this.gate, this, 4, this.q5));

                return StableCompositeDisposable.Create(s1, s2, s3, s4, s5, Disposable.Create(() =>
                {
                    lock (this.gate)
                    {
                        this.q1.Clear();
                        this.q2.Clear();
                        this.q3.Clear();
                        this.q4.Clear();
                        this.q5.Clear();
                    }
                }));
            }

            public override TR GetResult()
            {
                return this.parent.resultSelector(this.q1.Dequeue(), this.q2.Dequeue(), this.q3.Dequeue(), this.q4.Dequeue(), this.q5.Dequeue());
            }

            public override void OnNext(TR value)
            {
                this.observer.OnNext(value);
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


    internal class ZipObservable<T1, T2, T3, T4, T5, T6, TR> : OperatorObservableBase<TR>
    {
        IObservable<T1> source1;
        IObservable<T2> source2;
        IObservable<T3> source3;
        IObservable<T4> source4;
        IObservable<T5> source5;
        IObservable<T6> source6;
        ZipFunc<T1, T2, T3, T4, T5, T6, TR> resultSelector;

        public ZipObservable(
            IObservable<T1> source1,
            IObservable<T2> source2,
            IObservable<T3> source3,
            IObservable<T4> source4,
            IObservable<T5> source5,
            IObservable<T6> source6,
              ZipFunc<T1, T2, T3, T4, T5, T6, TR> resultSelector)
            : base(
                source1.IsRequiredSubscribeOnCurrentThread() ||
                source2.IsRequiredSubscribeOnCurrentThread() ||
                source3.IsRequiredSubscribeOnCurrentThread() ||
                source4.IsRequiredSubscribeOnCurrentThread() ||
                source5.IsRequiredSubscribeOnCurrentThread() ||
                source6.IsRequiredSubscribeOnCurrentThread() ||
                false)
        {
            this.source1 = source1;
            this.source2 = source2;
            this.source3 = source3;
            this.source4 = source4;
            this.source5 = source5;
            this.source6 = source6;
            this.resultSelector = resultSelector;
        }

        protected override IDisposable SubscribeCore(IObserver<TR> observer, IDisposable cancel)
        {
            return new Zip(this, observer, cancel).Run();
        }

        class Zip : NthZipObserverBase<TR>
        {
            readonly ZipObservable<T1, T2, T3, T4, T5, T6, TR> parent;
            readonly object gate = new object();
            readonly Queue<T1> q1 = new Queue<T1>();
            readonly Queue<T2> q2 = new Queue<T2>();
            readonly Queue<T3> q3 = new Queue<T3>();
            readonly Queue<T4> q4 = new Queue<T4>();
            readonly Queue<T5> q5 = new Queue<T5>();
            readonly Queue<T6> q6 = new Queue<T6>();

            public Zip(ZipObservable<T1, T2, T3, T4, T5, T6, TR> parent, IObserver<TR> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.SetQueue(new global::System.Collections.ICollection[] {this.q1, this.q2, this.q3, this.q4, this.q5, this.q6 });
                var s1 = this.parent.source1.Subscribe(new ZipObserver<T1>(this.gate, this, 0, this.q1));
                var s2 = this.parent.source2.Subscribe(new ZipObserver<T2>(this.gate, this, 1, this.q2));
                var s3 = this.parent.source3.Subscribe(new ZipObserver<T3>(this.gate, this, 2, this.q3));
                var s4 = this.parent.source4.Subscribe(new ZipObserver<T4>(this.gate, this, 3, this.q4));
                var s5 = this.parent.source5.Subscribe(new ZipObserver<T5>(this.gate, this, 4, this.q5));
                var s6 = this.parent.source6.Subscribe(new ZipObserver<T6>(this.gate, this, 5, this.q6));

                return StableCompositeDisposable.Create(s1, s2, s3, s4, s5, s6, Disposable.Create(() =>
                {
                    lock (this.gate)
                    {
                        this.q1.Clear();
                        this.q2.Clear();
                        this.q3.Clear();
                        this.q4.Clear();
                        this.q5.Clear();
                        this.q6.Clear();
                    }
                }));
            }

            public override TR GetResult()
            {
                return this.parent.resultSelector(this.q1.Dequeue(), this.q2.Dequeue(), this.q3.Dequeue(), this.q4.Dequeue(), this.q5.Dequeue(), this.q6.Dequeue());
            }

            public override void OnNext(TR value)
            {
                this.observer.OnNext(value);
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


    internal class ZipObservable<T1, T2, T3, T4, T5, T6, T7, TR> : OperatorObservableBase<TR>
    {
        IObservable<T1> source1;
        IObservable<T2> source2;
        IObservable<T3> source3;
        IObservable<T4> source4;
        IObservable<T5> source5;
        IObservable<T6> source6;
        IObservable<T7> source7;
        ZipFunc<T1, T2, T3, T4, T5, T6, T7, TR> resultSelector;

        public ZipObservable(
            IObservable<T1> source1,
            IObservable<T2> source2,
            IObservable<T3> source3,
            IObservable<T4> source4,
            IObservable<T5> source5,
            IObservable<T6> source6,
            IObservable<T7> source7,
              ZipFunc<T1, T2, T3, T4, T5, T6, T7, TR> resultSelector)
            : base(
                source1.IsRequiredSubscribeOnCurrentThread() ||
                source2.IsRequiredSubscribeOnCurrentThread() ||
                source3.IsRequiredSubscribeOnCurrentThread() ||
                source4.IsRequiredSubscribeOnCurrentThread() ||
                source5.IsRequiredSubscribeOnCurrentThread() ||
                source6.IsRequiredSubscribeOnCurrentThread() ||
                source7.IsRequiredSubscribeOnCurrentThread() ||
                false)
        {
            this.source1 = source1;
            this.source2 = source2;
            this.source3 = source3;
            this.source4 = source4;
            this.source5 = source5;
            this.source6 = source6;
            this.source7 = source7;
            this.resultSelector = resultSelector;
        }

        protected override IDisposable SubscribeCore(IObserver<TR> observer, IDisposable cancel)
        {
            return new Zip(this, observer, cancel).Run();
        }

        class Zip : NthZipObserverBase<TR>
        {
            readonly ZipObservable<T1, T2, T3, T4, T5, T6, T7, TR> parent;
            readonly object gate = new object();
            readonly Queue<T1> q1 = new Queue<T1>();
            readonly Queue<T2> q2 = new Queue<T2>();
            readonly Queue<T3> q3 = new Queue<T3>();
            readonly Queue<T4> q4 = new Queue<T4>();
            readonly Queue<T5> q5 = new Queue<T5>();
            readonly Queue<T6> q6 = new Queue<T6>();
            readonly Queue<T7> q7 = new Queue<T7>();

            public Zip(ZipObservable<T1, T2, T3, T4, T5, T6, T7, TR> parent, IObserver<TR> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.SetQueue(new global::System.Collections.ICollection[] {this.q1, this.q2, this.q3, this.q4, this.q5, this.q6, this.q7 });
                var s1 = this.parent.source1.Subscribe(new ZipObserver<T1>(this.gate, this, 0, this.q1));
                var s2 = this.parent.source2.Subscribe(new ZipObserver<T2>(this.gate, this, 1, this.q2));
                var s3 = this.parent.source3.Subscribe(new ZipObserver<T3>(this.gate, this, 2, this.q3));
                var s4 = this.parent.source4.Subscribe(new ZipObserver<T4>(this.gate, this, 3, this.q4));
                var s5 = this.parent.source5.Subscribe(new ZipObserver<T5>(this.gate, this, 4, this.q5));
                var s6 = this.parent.source6.Subscribe(new ZipObserver<T6>(this.gate, this, 5, this.q6));
                var s7 = this.parent.source7.Subscribe(new ZipObserver<T7>(this.gate, this, 6, this.q7));

                return StableCompositeDisposable.Create(s1, s2, s3, s4, s5, s6, s7, Disposable.Create(() =>
                {
                    lock (this.gate)
                    {
                        this.q1.Clear();
                        this.q2.Clear();
                        this.q3.Clear();
                        this.q4.Clear();
                        this.q5.Clear();
                        this.q6.Clear();
                        this.q7.Clear();
                    }
                }));
            }

            public override TR GetResult()
            {
                return this.parent.resultSelector(this.q1.Dequeue(), this.q2.Dequeue(), this.q3.Dequeue(), this.q4.Dequeue(), this.q5.Dequeue(), this.q6.Dequeue(), this.q7.Dequeue());
            }

            public override void OnNext(TR value)
            {
                this.observer.OnNext(value);
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

    #endregion

    // Nth infrastructure

    internal interface IZipObservable
    {
        void Dequeue(int index);
        void Fail(Exception error);
        void Done(int index);
    }

    internal abstract class NthZipObserverBase<T> : OperatorObserverBase<T, T>, IZipObservable
    {
        global::System.Collections.ICollection[] queues;
        bool[] isDone;
        int length;

        public NthZipObserverBase(IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
        {
        }

        protected void SetQueue(global::System.Collections.ICollection[] queues)
        {
            this.queues = queues;
            this.length = queues.Length;
            this.isDone = new bool[this.length];
        }

        public abstract T GetResult();

        // operators in lock
        public void Dequeue(int index)
        {
            var allQueueHasValue = true;
            for (int i = 0; i < this.length; i++)
            {
                if (this.queues[i].Count == 0)
                {
                    allQueueHasValue = false;
                    break;
                }
            }

            if (!allQueueHasValue)
            {
                var allCompletedWithoutSelf = true;
                for (int i = 0; i < this.length; i++)
                {
                    if (i == index) continue;
                    if (!this.isDone[i])
                    {
                        allCompletedWithoutSelf = false;
                        break;
                    }
                }

                if (allCompletedWithoutSelf)
                {
                    try {
                        this.observer.OnCompleted(); }
                    finally {
                        this.Dispose(); }
                    return;
                }
                else
                {
                    return;
                }
            }

            var result = default(T);
            try
            {
                result = this.GetResult();
            }
            catch (Exception ex)
            {
                try {
                    this.observer.OnError(ex); }
                finally {
                    this.Dispose(); }
                return;
            }

            this.OnNext(result);
        }

        public void Done(int index)
        {
            this.isDone[index] = true;
            var allTrue = true;
            for (int i = 0; i < this.length; i++)
            {
                if (!this.isDone[i])
                {
                    allTrue = false;
                    break;
                }
            }

            if (allTrue)
            {
                try {
                    this.observer.OnCompleted(); }
                finally {
                    this.Dispose(); }
            }
        }

        public void Fail(Exception error)
        {
            try {
                this.observer.OnError(error); }
            finally {
                this.Dispose(); }
        }
    }


    // nth
    internal class ZipObserver<T> : IObserver<T>
    {
        readonly object gate;
        readonly IZipObservable parent;
        readonly int index;
        readonly Queue<T> queue;

        public ZipObserver(object gate, IZipObservable parent, int index, Queue<T> queue)
        {
            this.gate = gate;
            this.parent = parent;
            this.index = index;
            this.queue = queue;
        }

        public void OnNext(T value)
        {
            lock (this.gate)
            {
                this.queue.Enqueue(value);
                this.parent.Dequeue(this.index);
            }
        }

        public void OnError(Exception error)
        {
            lock (this.gate)
            {
                this.parent.Fail(error);
            }
        }

        public void OnCompleted()
        {
            lock (this.gate)
            {
                this.parent.Done(this.index);
            }
        }
    }
}