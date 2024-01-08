using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class AggregateObservable<TSource> : OperatorObservableBase<TSource>
    {
        readonly IObservable<TSource> source;
        readonly Func<TSource, TSource, TSource> accumulator;

        public AggregateObservable(IObservable<TSource> source, Func<TSource, TSource, TSource> accumulator)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.accumulator = accumulator;
        }

        protected override IDisposable SubscribeCore(IObserver<TSource> observer, IDisposable cancel)
        {
            return this.source.Subscribe(new Aggregate(this, observer, cancel));
        }

        class Aggregate : OperatorObserverBase<TSource, TSource>
        {
            readonly AggregateObservable<TSource> parent;
            TSource accumulation;
            bool seenValue;

            public Aggregate(AggregateObservable<TSource> parent, IObserver<TSource> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
                this.seenValue = false;
            }

            public override void OnNext(TSource value)
            {
                if (!this.seenValue)
                {
                    this.seenValue = true;
                    this.accumulation = value;
                }
                else
                {
                    try
                    {
                        this.accumulation = this.parent.accumulator(this.accumulation, value);
                    }
                    catch (Exception ex)
                    {
                        try {
                            this.observer.OnError(ex); }
                        finally {
                            this.Dispose(); }
                        return;
                    }
                }
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
                if (!this.seenValue)
                {
                    throw new InvalidOperationException("Sequence contains no elements.");
                }

                this.observer.OnNext(this.accumulation);
                try {
                    this.observer.OnCompleted(); }
                finally {
                    this.Dispose(); }
            }
        }
    }

    internal class AggregateObservable<TSource, TAccumulate> : OperatorObservableBase<TAccumulate>
    {
        readonly IObservable<TSource> source;
        readonly TAccumulate seed;
        readonly Func<TAccumulate, TSource, TAccumulate> accumulator;

        public AggregateObservable(IObservable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> accumulator)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.seed = seed;
            this.accumulator = accumulator;
        }

        protected override IDisposable SubscribeCore(IObserver<TAccumulate> observer, IDisposable cancel)
        {
            return this.source.Subscribe(new Aggregate(this, observer, cancel));
        }

        class Aggregate : OperatorObserverBase<TSource, TAccumulate>
        {
            readonly AggregateObservable<TSource, TAccumulate> parent;
            TAccumulate accumulation;

            public Aggregate(AggregateObservable<TSource, TAccumulate> parent, IObserver<TAccumulate> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
                this.accumulation = parent.seed;
            }

            public override void OnNext(TSource value)
            {
                try
                {
                    this.accumulation = this.parent.accumulator(this.accumulation, value);
                }
                catch (Exception ex)
                {
                    try {
                        this.observer.OnError(ex); }
                    finally {
                        this.Dispose(); }
                    return;
                }
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
                this.observer.OnNext(this.accumulation);
                try {
                    this.observer.OnCompleted(); }
                finally {
                    this.Dispose(); }
            }
        }
    }

    internal class AggregateObservable<TSource, TAccumulate, TResult> : OperatorObservableBase<TResult>
    {
        readonly IObservable<TSource> source;
        readonly TAccumulate seed;
        readonly Func<TAccumulate, TSource, TAccumulate> accumulator;
        readonly Func<TAccumulate, TResult> resultSelector;

        public AggregateObservable(IObservable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> accumulator, Func<TAccumulate, TResult> resultSelector)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.seed = seed;
            this.accumulator = accumulator;
            this.resultSelector = resultSelector;
        }

        protected override IDisposable SubscribeCore(IObserver<TResult> observer, IDisposable cancel)
        {
            return this.source.Subscribe(new Aggregate(this, observer, cancel));
        }

        class Aggregate : OperatorObserverBase<TSource, TResult>
        {
            readonly AggregateObservable<TSource, TAccumulate, TResult> parent;
            TAccumulate accumulation;

            public Aggregate(AggregateObservable<TSource, TAccumulate, TResult> parent, IObserver<TResult> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
                this.accumulation = parent.seed;
            }

            public override void OnNext(TSource value)
            {
                try
                {
                    this.accumulation = this.parent.accumulator(this.accumulation, value);
                }
                catch (Exception ex)
                {
                    try {
                        this.observer.OnError(ex); }
                    finally {
                        this.Dispose(); }
                    return;
                }
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
                TResult result;
                try
                {
                    result = this.parent.resultSelector(this.accumulation);
                }
                catch (Exception ex)
                {
                    this.OnError(ex);
                    return;
                }

                this.observer.OnNext(result);
                try {
                    this.observer.OnCompleted(); }
                finally {
                    this.Dispose(); }
            }
        }
    }
}