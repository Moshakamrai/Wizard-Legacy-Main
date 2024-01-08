using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.InternalUtil;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    public abstract class OperatorObserverBase<TSource, TResult> : IDisposable, IObserver<TSource>
    {
        protected internal volatile IObserver<TResult> observer;
        IDisposable cancel;

        public OperatorObserverBase(IObserver<TResult> observer, IDisposable cancel)
        {
            this.observer = observer;
            this.cancel = cancel;
        }

        public abstract void OnNext(TSource value);

        public abstract void OnError(Exception error);

        public abstract void OnCompleted();

        public void Dispose()
        {
            this.observer = EmptyObserver<TResult>.Instance;
            var target = global::System.Threading.Interlocked.Exchange(ref this.cancel, null);
            if (target != null)
            {
                target.Dispose();
            }
        }
    }
}