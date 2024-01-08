using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class CastObservable<TSource, TResult> : OperatorObservableBase<TResult>
    {
        readonly IObservable<TSource> source;

        public CastObservable(IObservable<TSource> source)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
        }

        protected override IDisposable SubscribeCore(IObserver<TResult> observer, IDisposable cancel)
        {
            return this.source.Subscribe(new Cast(observer, cancel));
        }

        class Cast : OperatorObserverBase<TSource, TResult>
        {
            public Cast(IObserver<TResult> observer, IDisposable cancel)
                : base(observer, cancel)
            {
            }

            public override void OnNext(TSource value)
            {
                var castValue = default(TResult);
                try
                {
                    castValue = (TResult)(object)value;
                }
                catch (Exception ex)
                {
                    try {
                        this.observer.OnError(ex); }
                    finally {
                        this.Dispose(); }
                    return;
                }

                this.observer.OnNext(castValue);
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