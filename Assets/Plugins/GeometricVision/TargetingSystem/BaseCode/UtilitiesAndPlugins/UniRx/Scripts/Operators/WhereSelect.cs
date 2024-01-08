using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    // Optimize for .Where().Select()

    internal class WhereSelectObservable<T, TR> : OperatorObservableBase<TR>
    {
        readonly IObservable<T> source;
        readonly Func<T, bool> predicate;
        readonly Func<T, TR> selector;

        public WhereSelectObservable(IObservable<T> source, Func<T, bool> predicate, Func<T, TR> selector)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.predicate = predicate;
            this.selector = selector;
        }

        protected override IDisposable SubscribeCore(IObserver<TR> observer, IDisposable cancel)
        {
            return this.source.Subscribe(new WhereSelect(this, observer, cancel));
        }

        class WhereSelect : OperatorObserverBase<T, TR>
        {
            readonly WhereSelectObservable<T, TR> parent;

            public WhereSelect(WhereSelectObservable<T, TR> parent, IObserver<TR> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                this.parent = parent;
            }

            public override void OnNext(T value)
            {
                var isPassed = false;
                try
                {
                    isPassed = this.parent.predicate(value);
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