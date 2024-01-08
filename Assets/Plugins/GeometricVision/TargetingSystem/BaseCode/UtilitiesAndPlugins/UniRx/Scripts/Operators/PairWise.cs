using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class PairwiseObservable<T, TR> : OperatorObservableBase<TR>
    {
        readonly IObservable<T> source;
        readonly Func<T, T, TR> selector;

        public PairwiseObservable(IObservable<T> source, Func<T, T, TR> selector)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.selector = selector;
        }

        protected override IDisposable SubscribeCore(IObserver<TR> observer, IDisposable cancel)
        {
            return this.source.Subscribe(new Pairwise(this, observer, cancel));
        }

        class Pairwise : OperatorObserverBase<T, TR>
        {
            readonly PairwiseObservable<T, TR> parent;
            T prev = default(T);
            bool isFirst = true;

            public Pairwise(PairwiseObservable<T, TR> parent, IObserver<TR> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                this.parent = parent;
            }

            public override void OnNext(T value)
            {
                if (this.isFirst)
                {
                    this.isFirst = false;
                    this.prev = value;
                    return;
                }

                TR v;
                try
                {
                    v = this.parent.selector(this.prev, value);
                    this.prev = value;
                }
                catch (Exception ex)
                {
                    try {
                        this.observer.OnError(ex); } finally {
                        this.Dispose(); }
                    return;
                }

                this.observer.OnNext(v);
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
    }

    internal class PairwiseObservable<T> : OperatorObservableBase<Pair<T>>
    {
        readonly IObservable<T> source;

        public PairwiseObservable(IObservable<T> source)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
        }

        protected override IDisposable SubscribeCore(IObserver<Pair<T>> observer, IDisposable cancel)
        {
            return this.source.Subscribe(new Pairwise(observer, cancel));
        }

        class Pairwise : OperatorObserverBase<T, Pair<T>>
        {
            T prev = default(T);
            bool isFirst = true;

            public Pairwise(IObserver<Pair<T>> observer, IDisposable cancel)
                : base(observer, cancel)
            {
            }

            public override void OnNext(T value)
            {
                if (this.isFirst)
                {
                    this.isFirst = false;
                    this.prev = value;
                    return;
                }

                var pair = new Pair<T>(this.prev, value);
                this.prev = value;
                this.observer.OnNext(pair);
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
    }
}