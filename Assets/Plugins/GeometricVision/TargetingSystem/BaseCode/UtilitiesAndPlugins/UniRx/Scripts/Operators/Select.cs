using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal interface ISelect<TR>
    {
        // IObservable<TR2> CombineSelector<TR2>(Func<TR, TR2> selector);
        IObservable<TR> CombinePredicate(Func<TR, bool> predicate);
    }

    internal class SelectObservable<T, TR> : OperatorObservableBase<TR>, ISelect<TR>
    {
        readonly IObservable<T> source;
        readonly Func<T, TR> selector;
        readonly Func<T, int, TR> selectorWithIndex;

        public SelectObservable(IObservable<T> source, Func<T, TR> selector)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.selector = selector;
        }

        public SelectObservable(IObservable<T> source, Func<T, int, TR> selector)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.selectorWithIndex = selector;
        }

        // sometimes cause "which no ahead of time (AOT) code was generated." on IL2CPP...

        //public IObservable<TR2> CombineSelector<TR2>(Func<TR, TR2> combineSelector)
        //{
        //    if (this.selector != null)
        //    {
        //        return new Select<T, TR2>(source, x => combineSelector(this.selector(x)));
        //    }
        //    else
        //    {
        //        return new Select<TR, TR2>(this, combineSelector);
        //    }
        //}

        public IObservable<TR> CombinePredicate(Func<TR, bool> predicate)
        {
            if (this.selector != null)
            {
                return new SelectWhereObservable<T, TR>(this.source, this.selector, predicate);
            }
            else
            {
                return new WhereObservable<TR>(this, predicate); // can't combine
            }
        }

        protected override IDisposable SubscribeCore(IObserver<TR> observer, IDisposable cancel)
        {
            if (this.selector != null)
            {
                return this.source.Subscribe(new Select(this, observer, cancel));
            }
            else
            {
                return this.source.Subscribe(new Select_(this, observer, cancel));
            }
        }

        class Select : OperatorObserverBase<T, TR>
        {
            readonly SelectObservable<T, TR> parent;

            public Select(SelectObservable<T, TR> parent, IObserver<TR> observer, IDisposable cancel)
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

                this.observer.OnNext(v);
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

        // with Index
        class Select_ : OperatorObserverBase<T, TR>
        {
            readonly SelectObservable<T, TR> parent;
            int index;

            public Select_(SelectObservable<T, TR> parent, IObserver<TR> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                this.parent = parent;
                this.index = 0;
            }

            public override void OnNext(T value)
            {
                var v = default(TR);
                try
                {
                    v = this.parent.selectorWithIndex(value, this.index++);
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