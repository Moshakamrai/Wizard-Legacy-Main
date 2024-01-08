using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class TakeUntilObservable<T, TOther> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly IObservable<TOther> other;

        public TakeUntilObservable(IObservable<T> source, IObservable<TOther> other)
            : base(source.IsRequiredSubscribeOnCurrentThread() || other.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.other = other;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return new TakeUntil(this, observer, cancel).Run();
        }

        class TakeUntil : OperatorObserverBase<T, T>
        {
            readonly TakeUntilObservable<T, TOther> parent;
            object gate = new object();

            public TakeUntil(TakeUntilObservable<T, TOther> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                var otherSubscription = new SingleAssignmentDisposable();
                var otherObserver = new TakeUntilOther(this, otherSubscription);
                otherSubscription.Disposable = this.parent.other.Subscribe(otherObserver);

                var sourceSubscription = this.parent.source.Subscribe(this);

                return StableCompositeDisposable.Create(otherSubscription, sourceSubscription);
            }

            public override void OnNext(T value)
            {
                lock (this.gate)
                {
                    this.observer.OnNext(value);
                }
            }

            public override void OnError(Exception error)
            {
                lock (this.gate)
                {
                    try {
                        this.observer.OnError(error); } finally {
                        this.Dispose(); }
                }
            }

            public override void OnCompleted()
            {
                lock (this.gate)
                {
                    try {
                        this.observer.OnCompleted(); } finally {
                        this.Dispose(); }
                }
            }

            class TakeUntilOther : IObserver<TOther>
            {
                readonly TakeUntil sourceObserver;
                readonly IDisposable subscription;

                public TakeUntilOther(TakeUntil sourceObserver, IDisposable subscription)
                {
                    this.sourceObserver = sourceObserver;
                    this.subscription = subscription;
                }

                public void OnNext(TOther value)
                {
                    lock (this.sourceObserver.gate)
                    {
                        try
                        {
                            this.sourceObserver.observer.OnCompleted();
                        }
                        finally
                        {
                            this.sourceObserver.Dispose();
                            this.subscription.Dispose();
                        }
                    }
                }

                public void OnError(Exception error)
                {
                    lock (this.sourceObserver.gate)
                    {
                        try
                        {
                            this.sourceObserver.observer.OnError(error);
                        }
                        finally
                        {
                            this.sourceObserver.Dispose();
                            this.subscription.Dispose();
                        }
                    }
                }

                public void OnCompleted()
                {
                    lock (this.sourceObserver.gate)
                    {
                        try
                        {
                            this.sourceObserver.observer.OnCompleted();
                        }
                        finally
                        {
                            this.sourceObserver.Dispose();
                            this.subscription.Dispose();
                        }
                    }
                }
            }
        }
    }
}