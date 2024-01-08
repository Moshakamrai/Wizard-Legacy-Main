using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class CreateObservable<T> : OperatorObservableBase<T>
    {
        readonly Func<IObserver<T>, IDisposable> subscribe;

        public CreateObservable(Func<IObserver<T>, IDisposable> subscribe)
            : base(true) // fail safe
        {
            this.subscribe = subscribe;
        }

        public CreateObservable(Func<IObserver<T>, IDisposable> subscribe, bool isRequiredSubscribeOnCurrentThread)
            : base(isRequiredSubscribeOnCurrentThread)
        {
            this.subscribe = subscribe;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            observer = new Create(observer, cancel);
            return this.subscribe(observer) ?? Disposable.Empty;
        }

        class Create : OperatorObserverBase<T, T>
        {
            public Create(IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
            }

            public override void OnNext(T value)
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

    internal class CreateObservable<T, TState> : OperatorObservableBase<T>
    {
        readonly TState state;
        readonly Func<TState, IObserver<T>, IDisposable> subscribe;

        public CreateObservable(TState state, Func<TState, IObserver<T>, IDisposable> subscribe)
            : base(true) // fail safe
        {
            this.state = state;
            this.subscribe = subscribe;
        }

        public CreateObservable(TState state, Func<TState, IObserver<T>, IDisposable> subscribe, bool isRequiredSubscribeOnCurrentThread)
            : base(isRequiredSubscribeOnCurrentThread)
        {
            this.state = state;
            this.subscribe = subscribe;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            observer = new Create(observer, cancel);
            return this.subscribe(this.state, observer) ?? Disposable.Empty;
        }

        class Create : OperatorObserverBase<T, T>
        {
            public Create(IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
            }

            public override void OnNext(T value)
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

    internal class CreateSafeObservable<T> : OperatorObservableBase<T>
    {
        readonly Func<IObserver<T>, IDisposable> subscribe;

        public CreateSafeObservable(Func<IObserver<T>, IDisposable> subscribe)
            : base(true) // fail safe
        {
            this.subscribe = subscribe;
        }

        public CreateSafeObservable(Func<IObserver<T>, IDisposable> subscribe, bool isRequiredSubscribeOnCurrentThread)
            : base(isRequiredSubscribeOnCurrentThread)
        {
            this.subscribe = subscribe;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            observer = new CreateSafe(observer, cancel);
            return this.subscribe(observer) ?? Disposable.Empty;
        }

        class CreateSafe : OperatorObserverBase<T, T>
        {
            public CreateSafe(IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
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
                    this.Dispose(); // safe
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