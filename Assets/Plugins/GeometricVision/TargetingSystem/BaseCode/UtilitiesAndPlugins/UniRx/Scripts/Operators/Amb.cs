using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.InternalUtil;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class AmbObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly IObservable<T> second;

        public AmbObservable(IObservable<T> source, IObservable<T> second)
            : base(source.IsRequiredSubscribeOnCurrentThread() || second.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.second = second;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return new AmbOuterObserver(this, observer, cancel).Run();
        }

        class AmbOuterObserver : OperatorObserverBase<T, T>
        {
            enum AmbState
            {
                Left, Right, Neither
            }

            readonly AmbObservable<T> parent;
            readonly object gate = new object();
            SingleAssignmentDisposable leftSubscription;
            SingleAssignmentDisposable rightSubscription;
            AmbState choice = AmbState.Neither;

            public AmbOuterObserver(AmbObservable<T> parent, IObserver<T> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.leftSubscription = new SingleAssignmentDisposable();
                this.rightSubscription = new SingleAssignmentDisposable();
                var d = StableCompositeDisposable.Create(this.leftSubscription, this.rightSubscription);

                var left = new Amb();
                left.targetDisposable = d;
                left.targetObserver = new AmbDecisionObserver(this, AmbState.Left, this.rightSubscription, left);

                var right = new Amb();
                right.targetDisposable = d;
                right.targetObserver = new AmbDecisionObserver(this, AmbState.Right, this.leftSubscription, right);

                this.leftSubscription.Disposable = this.parent.source.Subscribe(left);
                this.rightSubscription.Disposable = this.parent.second.Subscribe(right);

                return d;
            }

            public override void OnNext(T value)
            {
                // no use
            }

            public override void OnError(Exception error)
            {
                // no use
            }

            public override void OnCompleted()
            {
                // no use
            }

            class Amb : IObserver<T>
            {
                public IObserver<T> targetObserver;
                public IDisposable targetDisposable;

                public void OnNext(T value)
                {
                    this.targetObserver.OnNext(value);
                }

                public void OnError(Exception error)
                {
                    try
                    {
                        this.targetObserver.OnError(error);
                    }
                    finally
                    {
                        this.targetObserver = EmptyObserver<T>.Instance;
                        this.targetDisposable.Dispose();
                    }
                }

                public void OnCompleted()
                {
                    try
                    {
                        this.targetObserver.OnCompleted();
                    }
                    finally
                    {
                        this.targetObserver = EmptyObserver<T>.Instance;
                        this.targetDisposable.Dispose();
                    }
                }
            }

            class AmbDecisionObserver : IObserver<T>
            {
                readonly AmbOuterObserver parent;
                readonly AmbState me;
                readonly IDisposable otherSubscription;
                readonly Amb self;

                public AmbDecisionObserver(AmbOuterObserver parent, AmbState me, IDisposable otherSubscription, Amb self)
                {
                    this.parent = parent;
                    this.me = me;
                    this.otherSubscription = otherSubscription;
                    this.self = self;
                }

                public void OnNext(T value)
                {
                    lock (this.parent.gate)
                    {
                        if (this.parent.choice == AmbState.Neither)
                        {
                            this.parent.choice = this.me;
                            this.otherSubscription.Dispose();
                            this.self.targetObserver = this.parent.observer;
                        }

                        if (this.parent.choice == this.me) this.self.targetObserver.OnNext(value);
                    }
                }

                public void OnError(Exception error)
                {
                    lock (this.parent.gate)
                    {
                        if (this.parent.choice == AmbState.Neither)
                        {
                            this.parent.choice = this.me;
                            this.otherSubscription.Dispose();
                            this.self.targetObserver = this.parent.observer;
                        }

                        if (this.parent.choice == this.me)
                        {
                            this.self.targetObserver.OnError(error);
                        }
                    }
                }

                public void OnCompleted()
                {
                    lock (this.parent.gate)
                    {
                        if (this.parent.choice == AmbState.Neither)
                        {
                            this.parent.choice = this.me;
                            this.otherSubscription.Dispose();
                            this.self.targetObserver = this.parent.observer;
                        }

                        if (this.parent.choice == this.me)
                        {
                            this.self.targetObserver.OnCompleted();
                        }
                    }
                }
            }
        }
    }
}