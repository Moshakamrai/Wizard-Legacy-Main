using System;
using System.Collections.Generic;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    public delegate TR CombineLatestFunc<T1, T2, T3, TR>(T1 arg1, T2 arg2, T3 arg3);
    public delegate TR CombineLatestFunc<T1, T2, T3, T4, TR>(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
    public delegate TR CombineLatestFunc<T1, T2, T3, T4, T5, TR>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
    public delegate TR CombineLatestFunc<T1, T2, T3, T4, T5, T6, TR>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
    public delegate TR CombineLatestFunc<T1, T2, T3, T4, T5, T6, T7, TR>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);


    // binary
    internal class CombineLatestObservable<TLeft, TRight, TResult> : OperatorObservableBase<TResult>
    {
        readonly IObservable<TLeft> left;
        readonly IObservable<TRight> right;
        readonly Func<TLeft, TRight, TResult> selector;

        public CombineLatestObservable(IObservable<TLeft> left, IObservable<TRight> right, Func<TLeft, TRight, TResult> selector)
            : base(left.IsRequiredSubscribeOnCurrentThread() || right.IsRequiredSubscribeOnCurrentThread())
        {
            this.left = left;
            this.right = right;
            this.selector = selector;
        }

        protected override IDisposable SubscribeCore(IObserver<TResult> observer, IDisposable cancel)
        {
            return new CombineLatest(this, observer, cancel).Run();
        }

        class CombineLatest : OperatorObserverBase<TResult, TResult>
        {
            readonly CombineLatestObservable<TLeft, TRight, TResult> parent;
            readonly object gate = new object();

            TLeft leftValue = default(TLeft);
            bool leftStarted = false;
            bool leftCompleted = false;

            TRight rightValue = default(TRight);
            bool rightStarted = false;
            bool rightCompleted = false;

            public CombineLatest(CombineLatestObservable<TLeft, TRight, TResult> parent, IObserver<TResult> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                var l = this.parent.left.Subscribe(new LeftObserver(this));
                var r = this.parent.right.Subscribe(new RightObserver(this));

                return StableCompositeDisposable.Create(l, r);
            }

            // publish in lock
            public void Publish()
            {
                if ((this.leftCompleted && !this.leftStarted) || (this.rightCompleted && !this.rightStarted))
                {
                    try {
                        this.observer.OnCompleted(); }
                    finally {
                        this.Dispose(); }
                    return;
                }
                else if (!(this.leftStarted && this.rightStarted))
                {
                    return;
                }

                TResult v;
                try
                {
                    v = this.parent.selector(this.leftValue, this.rightValue);
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

            class LeftObserver : IObserver<TLeft>
            {
                readonly CombineLatest parent;

                public LeftObserver(CombineLatest parent)
                {
                    this.parent = parent;
                }

                public void OnNext(TLeft value)
                {
                    lock (this.parent.gate)
                    {
                        this.parent.leftStarted = true;
                        this.parent.leftValue = value;
                        this.parent.Publish();
                    }
                }

                public void OnError(Exception error)
                {
                    lock (this.parent.gate)
                    {
                        this.parent.OnError(error);
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

            class RightObserver : IObserver<TRight>
            {
                readonly CombineLatest parent;

                public RightObserver(CombineLatest parent)
                {
                    this.parent = parent;
                }


                public void OnNext(TRight value)
                {
                    lock (this.parent.gate)
                    {
                        this.parent.rightStarted = true;
                        this.parent.rightValue = value;
                        this.parent.Publish();
                    }
                }

                public void OnError(Exception error)
                {
                    lock (this.parent.gate)
                    {
                        this.parent.OnError(error);
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
    internal class CombineLatestObservable<T> : OperatorObservableBase<IList<T>>
    {
        readonly IObservable<T>[] sources;

        public CombineLatestObservable(IObservable<T>[] sources)
            : base(true)
        {
            this.sources = sources;
        }

        protected override IDisposable SubscribeCore(IObserver<IList<T>> observer, IDisposable cancel)
        {
            return new CombineLatest(this, observer, cancel).Run();
        }

        class CombineLatest : OperatorObserverBase<IList<T>, IList<T>>
        {
            readonly CombineLatestObservable<T> parent;
            readonly object gate = new object();

            int length;
            T[] values;
            bool[] isStarted;
            bool[] isCompleted;
            bool isAllValueStarted;

            public CombineLatest(CombineLatestObservable<T> parent, IObserver<IList<T>> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.length = this.parent.sources.Length;
                this.values = new T[this.length];
                this.isStarted = new bool[this.length];
                this.isCompleted = new bool[this.length];
                this.isAllValueStarted = false;

                var disposables = new IDisposable[this.length];
                for (int i = 0; i < this.length; i++)
                {
                    var source = this.parent.sources[i];
                    disposables[i] = source.Subscribe(new CombineLatestObserver(this, i));
                }

                return StableCompositeDisposable.CreateUnsafe(disposables);
            }

            // publish is in the lock
            void Publish(int index)
            {
                this.isStarted[index] = true;

                if (this.isAllValueStarted)
                {
                    this.OnNext(new List<T>(this.values));
                    return;
                }

                var allValueStarted = true;
                for (int i = 0; i < this.length; i++)
                {
                    if (!this.isStarted[i])
                    {
                        allValueStarted = false;
                        break;
                    }
                }

                this.isAllValueStarted = allValueStarted;

                if (this.isAllValueStarted)
                {
                    this.OnNext(new List<T>(this.values));
                    return;
                }
                else
                {
                    var allCompletedWithoutSelf = true;
                    for (int i = 0; i < this.length; i++)
                    {
                        if (i == index) continue;
                        if (!this.isCompleted[i])
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

            class CombineLatestObserver : IObserver<T>
            {
                readonly CombineLatest parent;
                readonly int index;

                public CombineLatestObserver(CombineLatest parent, int index)
                {
                    this.parent = parent;
                    this.index = index;
                }

                public void OnNext(T value)
                {
                    lock (this.parent.gate)
                    {
                        this.parent.values[this.index] = value;
                        this.parent.Publish(this.index);
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
                        this.parent.isCompleted[this.index] = true;

                        var allTrue = true;
                        for (int i = 0; i < this.parent.length; i++)
                        {
                            if (!this.parent.isCompleted[i])
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

    // generated from UniRx.Console.CombineLatestGenerator.tt
    #region NTH

    internal class CombineLatestObservable<T1, T2, T3, TR> : OperatorObservableBase<TR>
    {
        IObservable<T1> source1;
        IObservable<T2> source2;
        IObservable<T3> source3;
        CombineLatestFunc<T1, T2, T3, TR> resultSelector;

        public CombineLatestObservable(
            IObservable<T1> source1,
            IObservable<T2> source2,
            IObservable<T3> source3,
              CombineLatestFunc<T1, T2, T3, TR> resultSelector)
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
            return new CombineLatest(3, this, observer, cancel).Run();
        }

        class CombineLatest : NthCombineLatestObserverBase<TR>
        {
            readonly CombineLatestObservable<T1, T2, T3, TR> parent;
            readonly object gate = new object();
            CombineLatestObserver<T1> c1;
            CombineLatestObserver<T2> c2;
            CombineLatestObserver<T3> c3;

            public CombineLatest(int length, CombineLatestObservable<T1, T2, T3, TR> parent, IObserver<TR> observer, IDisposable cancel)
                : base(length, observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.c1 = new CombineLatestObserver<T1>(this.gate, this, 0);
                this.c2 = new CombineLatestObserver<T2>(this.gate, this, 1);
                this.c3 = new CombineLatestObserver<T3>(this.gate, this, 2);

                var s1 = this.parent.source1.Subscribe(this.c1);
                var s2 = this.parent.source2.Subscribe(this.c2);
                var s3 = this.parent.source3.Subscribe(this.c3);

                return StableCompositeDisposable.Create(s1, s2, s3);
            }

            public override TR GetResult()
            {
                return this.parent.resultSelector(this.c1.Value, this.c2.Value, this.c3.Value);
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


    internal class CombineLatestObservable<T1, T2, T3, T4, TR> : OperatorObservableBase<TR>
    {
        IObservable<T1> source1;
        IObservable<T2> source2;
        IObservable<T3> source3;
        IObservable<T4> source4;
        CombineLatestFunc<T1, T2, T3, T4, TR> resultSelector;

        public CombineLatestObservable(
            IObservable<T1> source1,
            IObservable<T2> source2,
            IObservable<T3> source3,
            IObservable<T4> source4,
              CombineLatestFunc<T1, T2, T3, T4, TR> resultSelector)
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
            return new CombineLatest(4, this, observer, cancel).Run();
        }

        class CombineLatest : NthCombineLatestObserverBase<TR>
        {
            readonly CombineLatestObservable<T1, T2, T3, T4, TR> parent;
            readonly object gate = new object();
            CombineLatestObserver<T1> c1;
            CombineLatestObserver<T2> c2;
            CombineLatestObserver<T3> c3;
            CombineLatestObserver<T4> c4;

            public CombineLatest(int length, CombineLatestObservable<T1, T2, T3, T4, TR> parent, IObserver<TR> observer, IDisposable cancel)
                : base(length, observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.c1 = new CombineLatestObserver<T1>(this.gate, this, 0);
                this.c2 = new CombineLatestObserver<T2>(this.gate, this, 1);
                this.c3 = new CombineLatestObserver<T3>(this.gate, this, 2);
                this.c4 = new CombineLatestObserver<T4>(this.gate, this, 3);

                var s1 = this.parent.source1.Subscribe(this.c1);
                var s2 = this.parent.source2.Subscribe(this.c2);
                var s3 = this.parent.source3.Subscribe(this.c3);
                var s4 = this.parent.source4.Subscribe(this.c4);

                return StableCompositeDisposable.Create(s1, s2, s3, s4);
            }

            public override TR GetResult()
            {
                return this.parent.resultSelector(this.c1.Value, this.c2.Value, this.c3.Value, this.c4.Value);
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


    internal class CombineLatestObservable<T1, T2, T3, T4, T5, TR> : OperatorObservableBase<TR>
    {
        IObservable<T1> source1;
        IObservable<T2> source2;
        IObservable<T3> source3;
        IObservable<T4> source4;
        IObservable<T5> source5;
        CombineLatestFunc<T1, T2, T3, T4, T5, TR> resultSelector;

        public CombineLatestObservable(
            IObservable<T1> source1,
            IObservable<T2> source2,
            IObservable<T3> source3,
            IObservable<T4> source4,
            IObservable<T5> source5,
              CombineLatestFunc<T1, T2, T3, T4, T5, TR> resultSelector)
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
            return new CombineLatest(5, this, observer, cancel).Run();
        }

        class CombineLatest : NthCombineLatestObserverBase<TR>
        {
            readonly CombineLatestObservable<T1, T2, T3, T4, T5, TR> parent;
            readonly object gate = new object();
            CombineLatestObserver<T1> c1;
            CombineLatestObserver<T2> c2;
            CombineLatestObserver<T3> c3;
            CombineLatestObserver<T4> c4;
            CombineLatestObserver<T5> c5;

            public CombineLatest(int length, CombineLatestObservable<T1, T2, T3, T4, T5, TR> parent, IObserver<TR> observer, IDisposable cancel)
                : base(length, observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.c1 = new CombineLatestObserver<T1>(this.gate, this, 0);
                this.c2 = new CombineLatestObserver<T2>(this.gate, this, 1);
                this.c3 = new CombineLatestObserver<T3>(this.gate, this, 2);
                this.c4 = new CombineLatestObserver<T4>(this.gate, this, 3);
                this.c5 = new CombineLatestObserver<T5>(this.gate, this, 4);

                var s1 = this.parent.source1.Subscribe(this.c1);
                var s2 = this.parent.source2.Subscribe(this.c2);
                var s3 = this.parent.source3.Subscribe(this.c3);
                var s4 = this.parent.source4.Subscribe(this.c4);
                var s5 = this.parent.source5.Subscribe(this.c5);

                return StableCompositeDisposable.Create(s1, s2, s3, s4, s5);
            }

            public override TR GetResult()
            {
                return this.parent.resultSelector(this.c1.Value, this.c2.Value, this.c3.Value, this.c4.Value, this.c5.Value);
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


    internal class CombineLatestObservable<T1, T2, T3, T4, T5, T6, TR> : OperatorObservableBase<TR>
    {
        IObservable<T1> source1;
        IObservable<T2> source2;
        IObservable<T3> source3;
        IObservable<T4> source4;
        IObservable<T5> source5;
        IObservable<T6> source6;
        CombineLatestFunc<T1, T2, T3, T4, T5, T6, TR> resultSelector;

        public CombineLatestObservable(
            IObservable<T1> source1,
            IObservable<T2> source2,
            IObservable<T3> source3,
            IObservable<T4> source4,
            IObservable<T5> source5,
            IObservable<T6> source6,
              CombineLatestFunc<T1, T2, T3, T4, T5, T6, TR> resultSelector)
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
            return new CombineLatest(6, this, observer, cancel).Run();
        }

        class CombineLatest : NthCombineLatestObserverBase<TR>
        {
            readonly CombineLatestObservable<T1, T2, T3, T4, T5, T6, TR> parent;
            readonly object gate = new object();
            CombineLatestObserver<T1> c1;
            CombineLatestObserver<T2> c2;
            CombineLatestObserver<T3> c3;
            CombineLatestObserver<T4> c4;
            CombineLatestObserver<T5> c5;
            CombineLatestObserver<T6> c6;

            public CombineLatest(int length, CombineLatestObservable<T1, T2, T3, T4, T5, T6, TR> parent, IObserver<TR> observer, IDisposable cancel)
                : base(length, observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.c1 = new CombineLatestObserver<T1>(this.gate, this, 0);
                this.c2 = new CombineLatestObserver<T2>(this.gate, this, 1);
                this.c3 = new CombineLatestObserver<T3>(this.gate, this, 2);
                this.c4 = new CombineLatestObserver<T4>(this.gate, this, 3);
                this.c5 = new CombineLatestObserver<T5>(this.gate, this, 4);
                this.c6 = new CombineLatestObserver<T6>(this.gate, this, 5);

                var s1 = this.parent.source1.Subscribe(this.c1);
                var s2 = this.parent.source2.Subscribe(this.c2);
                var s3 = this.parent.source3.Subscribe(this.c3);
                var s4 = this.parent.source4.Subscribe(this.c4);
                var s5 = this.parent.source5.Subscribe(this.c5);
                var s6 = this.parent.source6.Subscribe(this.c6);

                return StableCompositeDisposable.Create(s1, s2, s3, s4, s5, s6);
            }

            public override TR GetResult()
            {
                return this.parent.resultSelector(this.c1.Value, this.c2.Value, this.c3.Value, this.c4.Value, this.c5.Value, this.c6.Value);
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


    internal class CombineLatestObservable<T1, T2, T3, T4, T5, T6, T7, TR> : OperatorObservableBase<TR>
    {
        IObservable<T1> source1;
        IObservable<T2> source2;
        IObservable<T3> source3;
        IObservable<T4> source4;
        IObservable<T5> source5;
        IObservable<T6> source6;
        IObservable<T7> source7;
        CombineLatestFunc<T1, T2, T3, T4, T5, T6, T7, TR> resultSelector;

        public CombineLatestObservable(
            IObservable<T1> source1,
            IObservable<T2> source2,
            IObservable<T3> source3,
            IObservable<T4> source4,
            IObservable<T5> source5,
            IObservable<T6> source6,
            IObservable<T7> source7,
              CombineLatestFunc<T1, T2, T3, T4, T5, T6, T7, TR> resultSelector)
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
            return new CombineLatest(7, this, observer, cancel).Run();
        }

        class CombineLatest : NthCombineLatestObserverBase<TR>
        {
            readonly CombineLatestObservable<T1, T2, T3, T4, T5, T6, T7, TR> parent;
            readonly object gate = new object();
            CombineLatestObserver<T1> c1;
            CombineLatestObserver<T2> c2;
            CombineLatestObserver<T3> c3;
            CombineLatestObserver<T4> c4;
            CombineLatestObserver<T5> c5;
            CombineLatestObserver<T6> c6;
            CombineLatestObserver<T7> c7;

            public CombineLatest(int length, CombineLatestObservable<T1, T2, T3, T4, T5, T6, T7, TR> parent, IObserver<TR> observer, IDisposable cancel)
                : base(length, observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.c1 = new CombineLatestObserver<T1>(this.gate, this, 0);
                this.c2 = new CombineLatestObserver<T2>(this.gate, this, 1);
                this.c3 = new CombineLatestObserver<T3>(this.gate, this, 2);
                this.c4 = new CombineLatestObserver<T4>(this.gate, this, 3);
                this.c5 = new CombineLatestObserver<T5>(this.gate, this, 4);
                this.c6 = new CombineLatestObserver<T6>(this.gate, this, 5);
                this.c7 = new CombineLatestObserver<T7>(this.gate, this, 6);

                var s1 = this.parent.source1.Subscribe(this.c1);
                var s2 = this.parent.source2.Subscribe(this.c2);
                var s3 = this.parent.source3.Subscribe(this.c3);
                var s4 = this.parent.source4.Subscribe(this.c4);
                var s5 = this.parent.source5.Subscribe(this.c5);
                var s6 = this.parent.source6.Subscribe(this.c6);
                var s7 = this.parent.source7.Subscribe(this.c7);

                return StableCompositeDisposable.Create(s1, s2, s3, s4, s5, s6, s7);
            }

            public override TR GetResult()
            {
                return this.parent.resultSelector(this.c1.Value, this.c2.Value, this.c3.Value, this.c4.Value, this.c5.Value, this.c6.Value, this.c7.Value);
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

    internal interface ICombineLatestObservable
    {
        void Publish(int index);
        void Fail(Exception error);
        void Done(int index);
    }

    internal abstract class NthCombineLatestObserverBase<T> : OperatorObserverBase<T, T>, ICombineLatestObservable
    {
        readonly int length;
        bool isAllValueStarted;
        readonly bool[] isStarted;
        readonly bool[] isCompleted;

        public NthCombineLatestObserverBase(int length, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
        {
            this.length = length;
            this.isAllValueStarted = false;
            this.isStarted = new bool[length];
            this.isCompleted = new bool[length];
        }

        public abstract T GetResult();

        // operators in lock
        public void Publish(int index)
        {
            this.isStarted[index] = true;

            if (this.isAllValueStarted)
            {
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
                return;
            }

            var allValueStarted = true;
            for (int i = 0; i < this.length; i++)
            {
                if (!this.isStarted[i])
                {
                    allValueStarted = false;
                    break;
                }
            }

            this.isAllValueStarted = allValueStarted;

            if (this.isAllValueStarted)
            {
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
                return;
            }
            else
            {
                var allCompletedWithoutSelf = true;
                for (int i = 0; i < this.length; i++)
                {
                    if (i == index) continue;
                    if (!this.isCompleted[i])
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
        }

        public void Done(int index)
        {
            this.isCompleted[index] = true;

            var allTrue = true;
            for (int i = 0; i < this.length; i++)
            {
                if (!this.isCompleted[i])
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

    // Nth
    internal class CombineLatestObserver<T> : IObserver<T>
    {
        readonly object gate;
        readonly ICombineLatestObservable parent;
        readonly int index;
        T value;

        public T Value { get { return this.value; } }

        public CombineLatestObserver(object gate, ICombineLatestObservable parent, int index)
        {
            this.gate = gate;
            this.parent = parent;
            this.index = index;
        }

        public void OnNext(T value)
        {
            lock (this.gate)
            {
                this.value = value;
                this.parent.Publish(this.index);
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