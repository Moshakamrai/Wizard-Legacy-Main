using System;
using System.Collections.Generic;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Subjects;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class GroupedObservable<TKey, TElement> : IGroupedObservable<TKey, TElement>
    {
        readonly TKey key;
        readonly IObservable<TElement> subject;
        readonly RefCountDisposable refCount;

        public TKey Key
        {
            get { return this.key; }
        }

        public GroupedObservable(TKey key, ISubject<TElement> subject, RefCountDisposable refCount)
        {
            this.key = key;
            this.subject = subject;
            this.refCount = refCount;
        }

        public IDisposable Subscribe(IObserver<TElement> observer)
        {
            var release = this.refCount.GetDisposable();
            var subscription = this.subject.Subscribe(observer);
            return StableCompositeDisposable.Create(release, subscription);
        }
    }

    internal class GroupByObservable<TSource, TKey, TElement> : OperatorObservableBase<IGroupedObservable<TKey, TElement>>
    {
        readonly IObservable<TSource> source;
        readonly Func<TSource, TKey> keySelector;
        readonly Func<TSource, TElement> elementSelector;
        readonly int? capacity;
        readonly IEqualityComparer<TKey> comparer;

        public GroupByObservable(IObservable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, int? capacity, IEqualityComparer<TKey> comparer)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.keySelector = keySelector;
            this.elementSelector = elementSelector;
            this.capacity = capacity;
            this.comparer = comparer;
        }

        protected override IDisposable SubscribeCore(IObserver<IGroupedObservable<TKey, TElement>> observer, IDisposable cancel)
        {
            return new GroupBy(this, observer, cancel).Run();
        }

        class GroupBy : OperatorObserverBase<TSource, IGroupedObservable<TKey, TElement>>
        {
            readonly GroupByObservable<TSource, TKey, TElement> parent;
            readonly Dictionary<TKey, ISubject<TElement>> map;
            ISubject<TElement> nullKeySubject;

            CompositeDisposable groupDisposable;
            RefCountDisposable refCountDisposable;

            public GroupBy(GroupByObservable<TSource, TKey, TElement> parent, IObserver<IGroupedObservable<TKey, TElement>> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                this.parent = parent;
                if (parent.capacity.HasValue)
                {
                    this.map = new Dictionary<TKey, ISubject<TElement>>(parent.capacity.Value, parent.comparer);
                }
                else
                {
                    this.map = new Dictionary<TKey, ISubject<TElement>>(parent.comparer);
                }
            }

            public IDisposable Run()
            {
                this.groupDisposable = new CompositeDisposable();
                this.refCountDisposable = new RefCountDisposable(this.groupDisposable);

                this.groupDisposable.Add(this.parent.source.Subscribe(this));

                return this.refCountDisposable;
            }

            public override void OnNext(TSource value)
            {
                var key = default(TKey);
                try
                {
                    key = this.parent.keySelector(value);
                }
                catch (Exception exception)
                {
                    this.Error(exception);
                    return;
                }

                var fireNewMapEntry = false;
                var writer = default(ISubject<TElement>);
                try
                {
                    if (key == null)
                    {
                        if (this.nullKeySubject == null)
                        {
                            this.nullKeySubject = new Subject<TElement>();
                            fireNewMapEntry = true;
                        }

                        writer = this.nullKeySubject;
                    }
                    else
                    {
                        if (!this.map.TryGetValue(key, out writer))
                        {
                            writer = new Subject<TElement>();
                            this.map.Add(key, writer);
                            fireNewMapEntry = true;
                        }
                    }
                }
                catch (Exception exception)
                {
                    this.Error(exception);
                    return;
                }

                if (fireNewMapEntry)
                {
                    var group = new GroupedObservable<TKey, TElement>(key, writer, this.refCountDisposable);
                    this.observer.OnNext(group);
                }

                var element = default(TElement);
                try
                {
                    element = this.parent.elementSelector(value);
                }
                catch (Exception exception)
                {
                    this.Error(exception);
                    return;
                }

                writer.OnNext(element);
            }

            public override void OnError(Exception error)
            {
                this.Error(error);
            }

            public override void OnCompleted()
            {
                try
                {
                    if (this.nullKeySubject != null) this.nullKeySubject.OnCompleted();

                    foreach (var s in this.map.Values)
                    {
                        s.OnCompleted();
                    }

                    this.observer.OnCompleted();
                }
                finally
                {
                    this.Dispose();
                }
            }

            void Error(Exception exception)
            {
                try
                {
                    if (this.nullKeySubject != null) this.nullKeySubject.OnError(exception);

                    foreach (var s in this.map.Values)
                    {
                        s.OnError(exception);
                    }

                    this.observer.OnError(exception);
                }
                finally
                {
                    this.Dispose();
                }
            }
        }
    }
}