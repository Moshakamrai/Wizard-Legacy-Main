using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    // Do, DoOnError, DoOnCompleted, DoOnTerminate, DoOnSubscribe, DoOnCancel

    internal class DoObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly Action<T> onNext;
        readonly Action<Exception> onError;
        readonly Action onCompleted;

        public DoObservable(IObservable<T> source, Action<T> onNext, Action<Exception> onError, Action onCompleted)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.onNext = onNext;
            this.onError = onError;
            this.onCompleted = onCompleted;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return new Do(this, observer, cancel).Run();
        }

        class Do : OperatorObserverBase<T, T>
        {
            readonly DoObservable<T> parent;

            public Do(DoObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                return this.parent.source.Subscribe(this);
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
                        this.observer.OnError(ex); } finally {
                        this.Dispose(); };
                    return;
                }
                this.observer.OnNext(value);
            }

            public override void OnError(Exception error)
            {
                try
                {
                    this.parent.onError(error);
                }
                catch (Exception ex)
                {
                    try {
                        this.observer.OnError(ex); } finally {
                        this.Dispose(); };
                    return;
                }
                try {
                    this.observer.OnError(error); } finally {
                    this.Dispose(); };
            }

            public override void OnCompleted()
            {
                try
                {
                    this.parent.onCompleted();
                }
                catch (Exception ex)
                {
                    this.observer.OnError(ex);
                    this.Dispose();
                    return;
                }
                try {
                    this.observer.OnCompleted(); } finally {
                    this.Dispose(); };
            }
        }
    }

    internal class DoObserverObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly IObserver<T> observer;

        public DoObserverObservable(IObservable<T> source, IObserver<T> observer)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.observer = observer;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return new Do(this, observer, cancel).Run();
        }

        class Do : OperatorObserverBase<T, T>
        {
            readonly DoObserverObservable<T> parent;

            public Do(DoObserverObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                return this.parent.source.Subscribe(this);
            }

            public override void OnNext(T value)
            {
                try
                {
                    this.parent.observer.OnNext(value);
                }
                catch (Exception ex)
                {
                    try {
                        this.observer.OnError(ex); }
                    finally {
                        this.Dispose(); }
                    return;
                }
                this.observer.OnNext(value);
            }

            public override void OnError(Exception error)
            {
                try
                {
                    this.parent.observer.OnError(error);
                }
                catch (Exception ex)
                {
                    try {
                        this.observer.OnError(ex); }
                    finally {
                        this.Dispose(); }
                    return;
                }

                try {
                    this.observer.OnError(error); }
                finally {
                    this.Dispose(); }
            }

            public override void OnCompleted()
            {
                try
                {
                    this.parent.observer.OnCompleted();
                }
                catch (Exception ex)
                {
                    try {
                        this.observer.OnError(ex); }
                    finally {
                        this.Dispose(); }
                    return;
                }

                try {
                    this.observer.OnCompleted(); }
                finally {
                    this.Dispose(); }
            }
        }
    }

    internal class DoOnErrorObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly Action<Exception> onError;

        public DoOnErrorObservable(IObservable<T> source, Action<Exception> onError)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.onError = onError;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return new DoOnError(this, observer, cancel).Run();
        }

        class DoOnError : OperatorObserverBase<T, T>
        {
            readonly DoOnErrorObservable<T> parent;

            public DoOnError(DoOnErrorObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                return this.parent.source.Subscribe(this);
            }

            public override void OnNext(T value)
            {
                this.observer.OnNext(value);
            }

            public override void OnError(Exception error)
            {
                try
                {
                    this.parent.onError(error);
                }
                catch (Exception ex)
                {
                    try {
                        this.observer.OnError(ex); }
                    finally {
                        this.Dispose(); }
                    return;
                }


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

    internal class DoOnCompletedObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly Action onCompleted;

        public DoOnCompletedObservable(IObservable<T> source, Action onCompleted)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.onCompleted = onCompleted;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return new DoOnCompleted(this, observer, cancel).Run();
        }

        class DoOnCompleted : OperatorObserverBase<T, T>
        {
            readonly DoOnCompletedObservable<T> parent;

            public DoOnCompleted(DoOnCompletedObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                return this.parent.source.Subscribe(this);
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
                try
                {
                    this.parent.onCompleted();
                }
                catch (Exception ex)
                {
                    this.observer.OnError(ex);
                    this.Dispose();
                    return;
                }
                try {
                    this.observer.OnCompleted(); } finally {
                    this.Dispose(); };
            }
        }
    }

    internal class DoOnTerminateObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly Action onTerminate;

        public DoOnTerminateObservable(IObservable<T> source, Action onTerminate)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.onTerminate = onTerminate;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return new DoOnTerminate(this, observer, cancel).Run();
        }

        class DoOnTerminate : OperatorObserverBase<T, T>
        {
            readonly DoOnTerminateObservable<T> parent;

            public DoOnTerminate(DoOnTerminateObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                return this.parent.source.Subscribe(this);
            }

            public override void OnNext(T value)
            {
                this.observer.OnNext(value);
            }

            public override void OnError(Exception error)
            {
                try
                {
                    this.parent.onTerminate();
                }
                catch (Exception ex)
                {
                    try {
                        this.observer.OnError(ex); }
                    finally {
                        this.Dispose(); }
                    return;
                }
                try {
                    this.observer.OnError(error); } finally {
                    this.Dispose(); };
            }

            public override void OnCompleted()
            {
                try
                {
                    this.parent.onTerminate();
                }
                catch (Exception ex)
                {
                    this.observer.OnError(ex);
                    this.Dispose();
                    return;
                }
                try {
                    this.observer.OnCompleted(); } finally {
                    this.Dispose(); };
            }
        }
    }

    internal class DoOnSubscribeObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly Action onSubscribe;

        public DoOnSubscribeObservable(IObservable<T> source, Action onSubscribe)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.onSubscribe = onSubscribe;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return new DoOnSubscribe(this, observer, cancel).Run();
        }

        class DoOnSubscribe : OperatorObserverBase<T, T>
        {
            readonly DoOnSubscribeObservable<T> parent;

            public DoOnSubscribe(DoOnSubscribeObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                try
                {
                    this.parent.onSubscribe();
                }
                catch (Exception ex)
                {
                    try {
                        this.observer.OnError(ex); }
                    finally {
                        this.Dispose(); }
                    return Disposable.Empty;
                }

                return this.parent.source.Subscribe(this);
            }

            public override void OnNext(T value)
            {
                this.observer.OnNext(value);
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

    internal class DoOnCancelObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly Action onCancel;

        public DoOnCancelObservable(IObservable<T> source, Action onCancel)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.onCancel = onCancel;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return new DoOnCancel(this, observer, cancel).Run();
        }

        class DoOnCancel : OperatorObserverBase<T, T>
        {
            readonly DoOnCancelObservable<T> parent;
            bool isCompletedCall = false;

            public DoOnCancel(DoOnCancelObservable<T> parent, IObserver<T> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                return StableCompositeDisposable.Create(this.parent.source.Subscribe(this), Disposable.Create(() =>
                {
                    if (!this.isCompletedCall)
                    {
                        this.parent.onCancel();
                    }
                }));
            }

            public override void OnNext(T value)
            {
                this.observer.OnNext(value);
            }

            public override void OnError(Exception error)
            {
                this.isCompletedCall = true;
                try {
                    this.observer.OnError(error); } finally {
                    this.Dispose(); };
            }

            public override void OnCompleted()
            {
                this.isCompletedCall = true;
                try {
                    this.observer.OnCompleted(); } finally {
                    this.Dispose(); };
            }
        }
    }
}