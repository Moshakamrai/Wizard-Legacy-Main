using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    // Optimize for .Select().Where()

    internal class SelectWhereObservable<T, TR> : OperatorObservableBase<TR>
    {
        readonly IObservable<T> source;
        readonly Func<T, TR> selector;
        readonly Func<TR, bool> predicate;

        public SelectWhereObservable(IObservable<T> source, Func<T, TR> selector, Func<TR, bool> predicate)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.selector = selector;
            this.predicate = predicate;
        }

        protected override IDisposable SubscribeCore(IObserver<TR> observer, IDisposable cancel)
        {
            return this.source.Subscribe(new SelectWhere(this, observer, cancel));
        }

        class SelectWhere : OperatorObserverBase<T, TR>
        {
            readonly SelectWhereObservable<T, TR> parent;

            public SelectWhere(SelectWhereObservable<T, TR> parent, IObserver<TR> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                this.parent = parent;
            }

            public override void OnNext(T value)
            {
                var v = default(TR);
                try
                {
                    v = this.parent.selector(value);
                }
                catch (Exception ex)
                {
                    try {
                        this.observer.OnError(ex); } finally {
                        this.Dispose(); }
                    return;
                }

                var isPassed = false;
                try
                {
                    isPassed = this.parent.predicate(v);
                }
                catch (Exception ex)
                {
                    try {
                        this.observer.OnError(ex); } finally {
                        this.Dispose(); }
                    return;
                }

                if (isPassed)
                {
                    this.observer.OnNext(v);
                }
            }

            public override void OnError(Exception error)
            {
                try {
                    this.observer.OnError(error); } finally {
                    this.Dispose(); }
            }

            public override void OnCompleted()
            {
                try {
                    this.observer.OnCompleted(); } finally {
                    this.Dispose(); }
            }
        }
    }
}