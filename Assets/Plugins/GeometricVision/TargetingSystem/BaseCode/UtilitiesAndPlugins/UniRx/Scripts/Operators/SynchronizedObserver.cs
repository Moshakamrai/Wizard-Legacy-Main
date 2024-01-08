using System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class SynchronizedObserver<T> : IObserver<T>
    {
        readonly IObserver<T> observer;
        readonly object gate;

        public SynchronizedObserver(IObserver<T> observer, object gate)
        {
            this.observer = observer;
            this.gate = gate;
        }

        public void OnNext(T value)
        {
            lock (this.gate)
            {
                this.observer.OnNext(value);
            }
        }

        public void OnError(Exception error)
        {
            lock (this.gate)
            {
                this.observer.OnError(error);
            }
        }

        public void OnCompleted()
        {
            lock (this.gate)
            {
                this.observer.OnCompleted();
            }
        }
    }
}
