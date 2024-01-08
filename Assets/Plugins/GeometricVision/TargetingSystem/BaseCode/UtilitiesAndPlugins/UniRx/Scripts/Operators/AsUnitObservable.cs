using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class AsUnitObservableObservable<T> : OperatorObservableBase<Unit>
    {
        readonly IObservable<T> source;

        public AsUnitObservableObservable(IObservable<T> source)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
        }

        protected override IDisposable SubscribeCore(IObserver<Unit> observer, IDisposable cancel)
        {
            return this.source.Subscribe(new AsUnitObservable(observer, cancel));
        }

        class AsUnitObservable : OperatorObserverBase<T, Unit>
        {
            public AsUnitObservable(IObserver<Unit> observer, IDisposable cancel)
                : base(observer, cancel)
            {
            }

            public override void OnNext(T value)
            {
                this.observer.OnNext(Unit.Default);
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