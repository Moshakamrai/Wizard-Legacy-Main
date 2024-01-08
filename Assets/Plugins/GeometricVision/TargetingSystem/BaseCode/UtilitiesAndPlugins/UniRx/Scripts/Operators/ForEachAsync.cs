using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class ForEachAsyncObservable<T> : OperatorObservableBase<Unit>
    {
        readonly IObservable<T> source;
        readonly Action<T> onNext;
        readonly Action<T, int> onNextWithIndex;

        public ForEachAsyncObservable(IObservable<T> source, Action<T> onNext)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.onNext = onNext;
        }

        public ForEachAsyncObservable(IObservable<T> source, Action<T, int> onNext)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.onNextWithIndex = onNext;
        }

        protected override IDisposable SubscribeCore(IObserver<Unit> observer, IDisposable cancel)
        {
            if (this.onNext != null)
            {
                return this.source.Subscribe(new ForEachAsync(this, observer, cancel));
            }
            else
            {
                return this.source.Subscribe(new ForEachAsync_(this, observer, cancel));
            }
        }

        class ForEachAsync : OperatorObserverBase<T, Unit>
        {
            readonly ForEachAsyncObservable<T> parent;

            public ForEachAsync(ForEachAsyncObservable<T> parent, IObserver<Unit> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public override void OnNext(T value)
            {
                try
                {
                    this.parent.onNext(value);
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
                this.observer.OnNext(Unit.Default);

                try {
                    this.observer.OnCompleted(); }
                finally {
                    this.Dispose(); }
            }
        }

        // with index
        class ForEachAsync_ : OperatorObserverBase<T, Unit>
        {
            readonly ForEachAsyncObservable<T> parent;
            int index = 0;

            public ForEachAsync_(ForEachAsyncObservable<T> parent, IObserver<Unit> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public override void OnNext(T value)
            {
                try
                {
                    this.parent.onNextWithIndex(value, this.index++);
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
                this.observer.OnNext(Unit.Default);

                try {
                    this.observer.OnCompleted(); }
                finally {
                    this.Dispose(); }
            }
        }
    }
}