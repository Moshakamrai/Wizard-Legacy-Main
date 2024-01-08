using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class ScanObservable<TSource> : OperatorObservableBase<TSource>
    {
        readonly IObservable<TSource> source;
        readonly Func<TSource, TSource, TSource> accumulator;

        public ScanObservable(IObservable<TSource> source, Func<TSource, TSource, TSource> accumulator)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.accumulator = accumulator;
        }

        protected override IDisposable SubscribeCore(IObserver<TSource> observer, IDisposable cancel)
        {
            return this.source.Subscribe(new Scan(this, observer, cancel));
        }

        class Scan : OperatorObserverBase<TSource, TSource>
        {
            readonly ScanObservable<TSource> parent;
            TSource accumulation;
            bool isFirst;

            public Scan(ScanObservable<TSource> parent, IObserver<TSource> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
                this.isFirst = true;
            }

            public override void OnNext(TSource value)
            {
                if (this.isFirst)
                {
                    this.isFirst = false;
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

                this.observer.OnNext(this.accumulation);
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

    internal class ScanObservable<TSource, TAccumulate> : OperatorObservableBase<TAccumulate>
    {
        readonly IObservable<TSource> source;
        readonly TAccumulate seed;
        readonly Func<TAccumulate, TSource, TAccumulate> accumulator;

        public ScanObservable(IObservable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> accumulator)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.seed = seed;
            this.accumulator = accumulator;
        }

        protected override IDisposable SubscribeCore(IObserver<TAccumulate> observer, IDisposable cancel)
        {
            return this.source.Subscribe(new Scan(this, observer, cancel));
        }

        class Scan : OperatorObserverBase<TSource, TAccumulate>
        {
            readonly ScanObservable<TSource, TAccumulate> parent;
            TAccumulate accumulation;
            bool isFirst;

            public Scan(ScanObservable<TSource, TAccumulate> parent, IObserver<TAccumulate> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
                this.isFirst = true;
            }

            public override void OnNext(TSource value)
            {
                if (this.isFirst)
                {
                    this.isFirst = false;
                    this.accumulation = this.parent.seed;
                }

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

                this.observer.OnNext(this.accumulation);
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