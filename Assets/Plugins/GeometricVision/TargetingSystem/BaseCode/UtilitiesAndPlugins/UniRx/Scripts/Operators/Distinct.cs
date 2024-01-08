using System;
using System.Collections.Generic;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class DistinctObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly IEqualityComparer<T> comparer;

        public DistinctObservable(IObservable<T> source, IEqualityComparer<T> comparer)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.comparer = comparer;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return this.source.Subscribe(new Distinct(this, observer, cancel));
        }

        class Distinct : OperatorObserverBase<T, T>
        {
            readonly HashSet<T> hashSet;

            public Distinct(DistinctObservable<T> parent, IObserver<T> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                this.hashSet = (parent.comparer == null)
                    ? new HashSet<T>()
                    : new HashSet<T>(parent.comparer);
            }

            public override void OnNext(T value)
            {
                var key = default(T);
                var isAdded = false;
                try
                {
                    key = value;
                    isAdded = this.hashSet.Add(key);
                }
                catch (Exception exception)
                {
                    try {
                        this.observer.OnError(exception); } finally {
                        this.Dispose(); }
                    return;
                }

                if (isAdded)
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

    internal class DistinctObservable<T, TKey> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly IEqualityComparer<TKey> comparer;
        readonly Func<T, TKey> keySelector;

        public DistinctObservable(IObservable<T> source, Func<T, TKey> keySelector, IEqualityComparer<TKey> comparer)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.comparer = comparer;
            this.keySelector = keySelector;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return this.source.Subscribe(new Distinct(this, observer, cancel));
        }

        class Distinct : OperatorObserverBase<T, T>
        {
            readonly DistinctObservable<T, TKey> parent;
            readonly HashSet<TKey> hashSet;

            public Distinct(DistinctObservable<T, TKey> parent, IObserver<T> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                this.parent = parent;
                this.hashSet = (parent.comparer == null)
                    ? new HashSet<TKey>()
                    : new HashSet<TKey>(parent.comparer);
            }

            public override void OnNext(T value)
            {
                var key = default(TKey);
                var isAdded = false;
                try
                {
                    key = this.parent.keySelector(value);
                    isAdded = this.hashSet.Add(key);
                }
                catch (Exception exception)
                {
                    try {
                        this.observer.OnError(exception); } finally {
                        this.Dispose(); }
                    return;
                }

                if (isAdded)
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