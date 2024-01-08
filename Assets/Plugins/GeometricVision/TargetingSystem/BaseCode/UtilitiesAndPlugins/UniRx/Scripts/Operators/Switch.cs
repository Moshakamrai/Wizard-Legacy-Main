using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class SwitchObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<IObservable<T>> sources;

        public SwitchObservable(IObservable<IObservable<T>> sources)
            : base(true)
        {
            this.sources = sources;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return new SwitchObserver(this, observer, cancel).Run();
        }

        class SwitchObserver : OperatorObserverBase<IObservable<T>, T>
        {
            readonly SwitchObservable<T> parent;

            readonly object gate = new object();
            readonly SerialDisposable innerSubscription = new SerialDisposable();
            bool isStopped = false;
            ulong latest = 0UL;
            bool hasLatest = false;

            public SwitchObserver(SwitchObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                var subscription = this.parent.sources.Subscribe(this);
                return StableCompositeDisposable.Create(subscription, this.innerSubscription);
            }

            public override void OnNext(IObservable<T> value)
            {
                var id = default(ulong);
                lock (this.gate)
                {
                    id = unchecked(++this.latest);
                    this.hasLatest = true;
                }

                var d = new SingleAssignmentDisposable();
                this.innerSubscription.Disposable = d;
                d.Disposable = value.Subscribe(new Switch(this, id));
            }

            public override void OnError(Exception error)
            {
                lock (this.gate)
                {
                    try {
                        this.observer.OnError(error); }
                    finally {
                        this.Dispose(); }
                }
            }

            public override void OnCompleted()
            {
                lock (this.gate)
                {
                    this.isStopped = true;
                    if (!this.hasLatest)
                    {
                        try {
                            this.observer.OnCompleted(); }
                        finally {
                            this.Dispose(); }
                    }
                }
            }

            class Switch : IObserver<T>
            {
                readonly SwitchObserver parent;
                readonly ulong id;

                public Switch(SwitchObserver observer, ulong id)
                {
                    this.parent = observer;
                    this.id = id;
                }

                public void OnNext(T value)
                {
                    lock (this.parent.gate)
                    {
                        if (this.parent.latest == this.id)
                        {
                            this.parent.observer.OnNext(value);
                        }
                    }
                }

                public void OnError(Exception error)
                {
                    lock (this.parent.gate)
                    {
                        if (this.parent.latest == this.id)
                        {
                            this.parent.observer.OnError(error);
                        }
                    }
                }

                public void OnCompleted()
                {
                    lock (this.parent.gate)
                    {
                        if (this.parent.latest == this.id)
                        {
                            this.parent.hasLatest = false;
                            if (this.parent.isStopped)
                            {
                                this.parent.observer.OnCompleted();
                            }
                        }
                    }
                }
            }
        }
    }
}
