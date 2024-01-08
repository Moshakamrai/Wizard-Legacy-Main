using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class MaterializeObservable<T> : OperatorObservableBase<Notification<T>>
    {
        readonly IObservable<T> source;

        public MaterializeObservable(IObservable<T> source)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
        }

        protected override IDisposable SubscribeCore(IObserver<Notification<T>> observer, IDisposable cancel)
        {
            return new Materialize(this, observer, cancel).Run();
        }

        class Materialize : OperatorObserverBase<T, Notification<T>>
        {
            readonly MaterializeObservable<T> parent;

            public Materialize(MaterializeObservable<T> parent, IObserver<Notification<T>> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                return this.parent.source.Subscribe(this);
            }

            public override void OnNext(T value)
            {
                this.observer.OnNext(Notification.CreateOnNext(value));
            }

            public override void OnError(Exception error)
            {
                this.observer.OnNext(Notification.CreateOnError<T>(error));
                try {
                    this.observer.OnCompleted(); } finally {
                    this.Dispose(); }
            }

            public override void OnCompleted()
            {
                this.observer.OnNext(Notification.CreateOnCompleted<T>());
                try {
                    this.observer.OnCompleted(); } finally {
                    this.Dispose(); }
            }
        }
    }
}