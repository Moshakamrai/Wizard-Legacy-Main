using System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class DeferObservable<T> : OperatorObservableBase<T>
    {
        readonly Func<IObservable<T>> observableFactory;

        public DeferObservable(Func<IObservable<T>> observableFactory)
            : base(false)
        {
            this.observableFactory = observableFactory;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            observer = new Defer(observer, cancel);

            IObservable<T> source;
            try
            {
                source = this.observableFactory();
            }
            catch (Exception ex)
            {
                source = Observable.Throw<T>(ex);
            }

            return source.Subscribe(observer);
        }

        class Defer : OperatorObserverBase<T, T>
        {
            public Defer(IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
            }

            public override void OnNext(T value)
            {
                try
                {
                    this.observer.OnNext(value);
                }
                catch
                {
                    this.Dispose();
                    throw;
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
                try {
                    this.observer.OnCompleted(); }
                finally {
                    this.Dispose(); }
            }
        }
    }
}