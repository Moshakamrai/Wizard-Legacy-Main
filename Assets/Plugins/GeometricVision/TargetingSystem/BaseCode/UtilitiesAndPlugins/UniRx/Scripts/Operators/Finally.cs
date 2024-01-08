using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class FinallyObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly Action finallyAction;

        public FinallyObservable(IObservable<T> source, Action finallyAction)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.finallyAction = finallyAction;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return new Finally(this, observer, cancel).Run();
        }

        class Finally : OperatorObserverBase<T, T>
        {
            readonly FinallyObservable<T> parent;

            public Finally(FinallyObservable<T> parent, IObserver<T> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                IDisposable subscription;
                try
                {
                    subscription = this.parent.source.Subscribe(this);
                }
                catch
                {
                    // This behaviour is not same as .NET Official Rx
                    this.parent.finallyAction();
                    throw;
                }

                return StableCompositeDisposable.Create(subscription, Disposable.Create(() =>
                {
                    this.parent.finallyAction();
                }));
            }

            public override void OnNext(T value)
            {
                this.observer.OnNext(value);
            }

            public override void OnError(Exception error)
            {
                try {
                    this.observer.OnError(error); } finally {
                    this.Dispose(); };
            }

            public override void OnCompleted()
            {
                try {
                    this.observer.OnCompleted(); } finally {
                    this.Dispose(); };
            }
        }
    }
}