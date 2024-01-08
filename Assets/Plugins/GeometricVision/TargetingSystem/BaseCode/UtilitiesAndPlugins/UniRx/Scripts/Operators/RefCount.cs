using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Subjects;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class RefCountObservable<T> : OperatorObservableBase<T>
    {
        readonly IConnectableObservable<T> source;
        readonly object gate = new object();
        int refCount = 0;
        IDisposable connection;

        public RefCountObservable(IConnectableObservable<T> source)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return new RefCount(this, observer, cancel).Run();
        }

        class RefCount : OperatorObserverBase<T, T>
        {
            readonly RefCountObservable<T> parent;

            public RefCount(RefCountObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                var subcription = this.parent.source.Subscribe(this);

                lock (this.parent.gate)
                {
                    if (++this.parent.refCount == 1)
                    {
                        this.parent.connection = this.parent.source.Connect();
                    }
                }

                return Disposable.Create(() =>
                {
                    subcription.Dispose();

                    lock (this.parent.gate)
                    {
                        if (--this.parent.refCount == 0)
                        {
                            this.parent.connection.Dispose();
                        }
                    }
                });
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
}