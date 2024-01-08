using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class ContinueWithObservable<TSource, TResult> : OperatorObservableBase<TResult>
    {
        readonly IObservable<TSource> source;
        readonly Func<TSource, IObservable<TResult>> selector;

        public ContinueWithObservable(IObservable<TSource> source, Func<TSource, IObservable<TResult>> selector)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.selector = selector;
        }

        protected override IDisposable SubscribeCore(IObserver<TResult> observer, IDisposable cancel)
        {
            return new ContinueWith(this, observer, cancel).Run();
        }

        class ContinueWith : OperatorObserverBase<TSource, TResult>
        {
            readonly ContinueWithObservable<TSource, TResult> parent;
            readonly SerialDisposable serialDisposable = new SerialDisposable();

            bool seenValue;
            TSource lastValue;

            public ContinueWith(ContinueWithObservable<TSource, TResult> parent, IObserver<TResult> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                var sourceDisposable = new SingleAssignmentDisposable();
                this.serialDisposable.Disposable = sourceDisposable;

                sourceDisposable.Disposable = this.parent.source.Subscribe(this);
                return this.serialDisposable;
            }

            public override void OnNext(TSource value)
            {
                this.seenValue = true;
                this.lastValue = value;
            }

            public override void OnError(Exception error)
            {
                try {
                    this.observer.OnError(error); } finally {
                    this.Dispose(); };
            }

            public override void OnCompleted()
            {
                if (this.seenValue)
                {
                    try
	                {
		                var v = this.parent.selector(this.lastValue);
		                // dispose source subscription
                        this.serialDisposable.Disposable = v.Subscribe(this.observer);
	                }
	                catch (Exception error)
	                {
                        this.OnError(error);
	                }
                }
                else
                {
                    try {
                        this.observer.OnCompleted(); } finally {
                        this.Dispose(); };
                }
            }
        }
    }
}