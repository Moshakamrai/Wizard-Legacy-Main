using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class WhereObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly Func<T, bool> predicate;
        readonly Func<T, int, bool> predicateWithIndex;

        public WhereObservable(IObservable<T> source, Func<T, bool> predicate)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.predicate = predicate;
        }

        public WhereObservable(IObservable<T> source, Func<T, int, bool> predicateWithIndex)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.predicateWithIndex = predicateWithIndex;
        }

        // Optimize for .Where().Where()

        public IObservable<T> CombinePredicate(Func<T, bool> combinePredicate)
        {
            if (this.predicate != null)
            {
                return new WhereObservable<T>(this.source, x => this.predicate(x) && combinePredicate(x));
            }
            else
            {
                return new WhereObservable<T>(this, combinePredicate);
            }
        }

        // Optimize for .Where().Select()

        public IObservable<TR> CombineSelector<TR>(Func<T, TR> selector)
        {
            if (this.predicate != null)
            {
                return new WhereSelectObservable<T, TR>(this.source, this.predicate, selector);
            }
            else
            {
                return new SelectObservable<T, TR>(this, selector); // can't combine
            }
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            if (this.predicate != null)
            {
                return this.source.Subscribe(new Where(this, observer, cancel));
            }
            else
            {
                return this.source.Subscribe(new Where_(this, observer, cancel));
            }
        }

        class Where : OperatorObserverBase<T, T>
        {
            readonly WhereObservable<T> parent;

            public Where(WhereObservable<T> parent, IObserver<T> observer, IDisposable cancel)
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
                    this.observer.OnNext(value);
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

        class Where_ : OperatorObserverBase<T, T>
        {
            readonly WhereObservable<T> parent;
            int index;

            public Where_(WhereObservable<T> parent, IObserver<T> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                this.parent = parent;
                this.index = 0;
            }

            public override void OnNext(T value)
            {
                var isPassed = false;
                try
                {
                    isPassed = this.parent.predicateWithIndex(value, this.index++);
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
                    this.observer.OnNext(value);
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