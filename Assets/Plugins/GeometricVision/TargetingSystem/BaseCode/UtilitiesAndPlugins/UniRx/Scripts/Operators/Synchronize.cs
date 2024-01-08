using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class SynchronizeObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly object gate;

        public SynchronizeObservable(IObservable<T> source, object gate)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.gate = gate;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return this.source.Subscribe(new Synchronize(this, observer, cancel));
        }

        class Synchronize : OperatorObserverBase<T, T>
        {
            readonly SynchronizeObservable<T> parent;

            public Synchronize(SynchronizeObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public override void OnNext(T value)
            {
                lock (this.parent.gate)
                {
                    this.observer.OnNext(value);
                }
            }

            public override void OnError(Exception error)
            {
                lock (this.parent.gate)
                {
                    try {
                        this.observer.OnError(error); } finally {
                        this.Dispose(); };
                }
            }

            public override void OnCompleted()
            {
                lock (this.parent.gate)
                {
                    try {
                        this.observer.OnCompleted(); } finally {
                        this.Dispose(); };
                }
            }
        }
    }
}