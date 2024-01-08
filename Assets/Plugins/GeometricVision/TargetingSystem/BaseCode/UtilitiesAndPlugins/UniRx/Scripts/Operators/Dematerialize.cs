using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class DematerializeObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<Notification<T>> source;

        public DematerializeObservable(IObservable<Notification<T>> source)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return new Dematerialize(this, observer, cancel).Run();
        }

        class Dematerialize : OperatorObserverBase<Notification<T>, T>
        {
            readonly DematerializeObservable<T> parent;

            public Dematerialize(DematerializeObservable<T> parent, IObserver<T> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                return this.parent.source.Subscribe(this); 
            }

            public override void OnNext(Notification<T> value)
            {
                switch (value.Kind)
                {
                    case NotificationKind.OnNext:
                        this.observer.OnNext(value.Value);
                        break;
                    case NotificationKind.OnError:
                        try {
                            this.observer.OnError(value.Exception); }
                        finally {
                            this.Dispose(); }
                        break;
                    case NotificationKind.OnCompleted:
                        try {
                            this.observer.OnCompleted(); }
                        finally {
                            this.Dispose(); }
                        break;
                    default:
                        break;
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
                try {
                    this.observer.OnCompleted(); } finally {
                    this.Dispose(); }
            }
        }
    }
}