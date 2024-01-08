using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.InternalUtil;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class Wait<T> : IObserver<T>
    {
        static readonly TimeSpan InfiniteTimeSpan = new TimeSpan(0, 0, 0, 0, -1); // from .NET 4.5

        readonly IObservable<T> source;
        readonly TimeSpan timeout;

        global::System.Threading.ManualResetEvent semaphore;

        bool seenValue = false;
        T value = default(T);
        Exception ex = default(Exception);

        public Wait(IObservable<T> source, TimeSpan timeout)
        {
            this.source = source;
            this.timeout = timeout;
        }

        public T Run()
        {
            this.semaphore = new global::System.Threading.ManualResetEvent(false);
            using (this.source.Subscribe(this))
            {
                var waitComplete = (this.timeout == InfiniteTimeSpan)
                    ? this.semaphore.WaitOne()
                    : this.semaphore.WaitOne(this.timeout);

                if (!waitComplete)
                {
                    throw new TimeoutException("OnCompleted not fired.");
                }
            }

            if (this.ex != null) this.ex.Throw();
            if (!this.seenValue) throw new InvalidOperationException("No Elements.");

            return this.value;
        }

        public void OnNext(T value)
        {
            this.seenValue = true;
            this.value = value;
        }

        public void OnError(Exception error)
        {
            this.ex = error;
            this.semaphore.Set();
        }

        public void OnCompleted()
        {
            this.semaphore.Set();
        }
    }
}