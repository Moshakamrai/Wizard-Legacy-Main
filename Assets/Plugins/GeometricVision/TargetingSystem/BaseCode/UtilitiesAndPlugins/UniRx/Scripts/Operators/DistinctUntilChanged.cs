using System;
using System.Collections.Generic;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class DistinctUntilChangedObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly IEqualityComparer<T> comparer;

        public DistinctUntilChangedObservable(IObservable<T> source, IEqualityComparer<T> comparer)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.comparer = comparer;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return this.source.Subscribe(new DistinctUntilChanged(this, observer, cancel));
        }

        class DistinctUntilChanged : OperatorObserverBase<T, T>
        {
            readonly DistinctUntilChangedObservable<T> parent;
            bool isFirst = true;
            T prevKey = default(T);

            public DistinctUntilChanged(DistinctUntilChangedObservable<T> parent, IObserver<T> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                this.parent = parent;
            }

            public override void OnNext(T value)
            {
                T currentKey;
                try
                {
                    currentKey = value;
                }
                catch (Exception exception)
                {
                    try {
                        this.observer.OnError(exception); } finally {
                        this.Dispose(); }
                    return;
                }

                var sameKey = false;
                if (this.isFirst)
                {
                    this.isFirst = false;
                }
                else
                {
                    try
                    {
                        sameKey = this.parent.comparer.Equals(currentKey, this.prevKey);
                    }
                    catch (Exception ex)
                    {
                        try {
                            this.observer.OnError(ex); } finally {
                            this.Dispose(); }
                        return;
                    }
                }

                if (!sameKey)
                {
                    this.prevKey = currentKey;
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

    internal class DistinctUntilChangedObservable<T, TKey> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly IEqualityComparer<TKey> comparer;
        readonly Func<T, TKey> keySelector;

        public DistinctUntilChangedObservable(IObservable<T> source, Func<T, TKey> keySelector, IEqualityComparer<TKey> comparer)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.comparer = comparer;
            this.keySelector = keySelector;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return this.source.Subscribe(new DistinctUntilChanged(this, observer, cancel));
        }

        class DistinctUntilChanged : OperatorObserverBase<T, T>
        {
            readonly DistinctUntilChangedObservable<T, TKey> parent;
            bool isFirst = true;
            TKey prevKey = default(TKey);

            public DistinctUntilChanged(DistinctUntilChangedObservable<T, TKey> parent, IObserver<T> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                this.parent = parent;
            }

            public override void OnNext(T value)
            {
                TKey currentKey;
                try
                {
                    currentKey = this.parent.keySelector(value);
                }
                catch (Exception exception)
                {
                    try {
                        this.observer.OnError(exception); } finally {
                        this.Dispose(); }
                    return;
                }

                var sameKey = false;
                if (this.isFirst)
                {
                    this.isFirst = false;
                }
                else
                {
                    try
                    {
                        sameKey = this.parent.comparer.Equals(currentKey, this.prevKey);
                    }
                    catch (Exception ex)
                    {
                        try {
                            this.observer.OnError(ex); } finally {
                            this.Dispose(); }
                        return;
                    }
                }

                if (!sameKey)
                {
                    this.prevKey = currentKey;
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