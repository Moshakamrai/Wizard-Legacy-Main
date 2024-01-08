using System;
using System.Collections.Generic;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class SelectManyObservable<TSource, TResult> : OperatorObservableBase<TResult>
    {
        readonly IObservable<TSource> source;
        readonly Func<TSource, IObservable<TResult>> selector;
        readonly Func<TSource, int, IObservable<TResult>> selectorWithIndex;
        readonly Func<TSource, IEnumerable<TResult>> selectorEnumerable;
        readonly Func<TSource, int, IEnumerable<TResult>> selectorEnumerableWithIndex;

        public SelectManyObservable(IObservable<TSource> source, Func<TSource, IObservable<TResult>> selector)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.selector = selector;
        }

        public SelectManyObservable(IObservable<TSource> source, Func<TSource, int, IObservable<TResult>> selector)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.selectorWithIndex = selector;
        }

        public SelectManyObservable(IObservable<TSource> source, Func<TSource, IEnumerable<TResult>> selector)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.selectorEnumerable = selector;
        }

        public SelectManyObservable(IObservable<TSource> source, Func<TSource, int, IEnumerable<TResult>> selector)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.selectorEnumerableWithIndex = selector;
        }

        protected override IDisposable SubscribeCore(IObserver<TResult> observer, IDisposable cancel)
        {
            if (this.selector != null)
            {
                return new SelectManyOuterObserver(this, observer, cancel).Run();
            }
            else if (this.selectorWithIndex != null)
            {
                return new SelectManyObserverWithIndex(this, observer, cancel).Run();
            }
            else if (this.selectorEnumerable != null)
            {
                return new SelectManyEnumerableObserver(this, observer, cancel).Run();
            }
            else if (this.selectorEnumerableWithIndex != null)
            {
                return new SelectManyEnumerableObserverWithIndex(this, observer, cancel).Run();
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        class SelectManyOuterObserver : OperatorObserverBase<TSource, TResult>
        {
            readonly SelectManyObservable<TSource, TResult> parent;

            CompositeDisposable collectionDisposable;
            SingleAssignmentDisposable sourceDisposable;
            object gate = new object();
            bool isStopped = false;

            public SelectManyOuterObserver(SelectManyObservable<TSource, TResult> parent, IObserver<TResult> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.collectionDisposable = new CompositeDisposable();
                this.sourceDisposable = new SingleAssignmentDisposable();
                this.collectionDisposable.Add(this.sourceDisposable);

                this.sourceDisposable.Disposable = this.parent.source.Subscribe(this);
                return this.collectionDisposable;
            }

            public override void OnNext(TSource value)
            {
                IObservable<TResult> nextObservable;
                try
                {
                    nextObservable = this.parent.selector(value);
                }
                catch (Exception ex)
                {
                    try {
                        this.observer.OnError(ex); } finally {
                        this.Dispose(); };
                    return;
                }

                var disposable = new SingleAssignmentDisposable();
                this.collectionDisposable.Add(disposable);
                var collectionObserver = new SelectMany(this, disposable);
                disposable.Disposable = nextObservable.Subscribe(collectionObserver);
            }

            public override void OnError(Exception error)
            {
                lock (this.gate)
                {
                    try {
                        this.observer.OnError(error); } finally {
                        this.Dispose(); };
                }
            }

            public override void OnCompleted()
            {
                this.isStopped = true;
                if (this.collectionDisposable.Count == 1)
                {
                    lock (this.gate)
                    {
                        try {
                            this.observer.OnCompleted(); } finally {
                            this.Dispose(); };
                    }
                }
                else
                {
                    this.sourceDisposable.Dispose();
                }
            }

            class SelectMany : OperatorObserverBase<TResult, TResult>
            {
                readonly SelectManyOuterObserver parent;
                readonly IDisposable cancel;

                public SelectMany(SelectManyOuterObserver parent, IDisposable cancel)
                    : base(parent.observer, cancel)
                {
                    this.parent = parent;
                    this.cancel = cancel;
                }

                public override void OnNext(TResult value)
                {
                    lock (this.parent.gate)
                    {
                        this.observer.OnNext(value);
                    }
                }

                public override void OnError(Exception error)
                {
                    lock (this.parent.gate)
                    {
                        try {
                            this.observer.OnError(error); } finally {
                            this.Dispose(); };
                    }
                }

                public override void OnCompleted()
                {
                    this.parent.collectionDisposable.Remove(this.cancel);
                    if (this.parent.isStopped && this.parent.collectionDisposable.Count == 1)
                    {
                        lock (this.parent.gate)
                        {
                            try {
                                this.observer.OnCompleted(); } finally {
                                this.Dispose(); };
                        }
                    }
                }
            }
        }

        class SelectManyObserverWithIndex : OperatorObserverBase<TSource, TResult>
        {
            readonly SelectManyObservable<TSource, TResult> parent;

            CompositeDisposable collectionDisposable;
            int index = 0;
            object gate = new object();
            bool isStopped = false;
            SingleAssignmentDisposable sourceDisposable;

            public SelectManyObserverWithIndex(SelectManyObservable<TSource, TResult> parent, IObserver<TResult> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.collectionDisposable = new CompositeDisposable();
                this.sourceDisposable = new SingleAssignmentDisposable();
                this.collectionDisposable.Add(this.sourceDisposable);

                this.sourceDisposable.Disposable = this.parent.source.Subscribe(this);
                return this.collectionDisposable;
            }

            public override void OnNext(TSource value)
            {
                IObservable<TResult> nextObservable;
                try
                {
                    nextObservable = this.parent.selectorWithIndex(value, this.index++);
                }
                catch (Exception ex)
                {
                    try {
                        this.observer.OnError(ex); } finally {
                        this.Dispose(); };
                    return;
                }

                var disposable = new SingleAssignmentDisposable();
                this.collectionDisposable.Add(disposable);
                var collectionObserver = new SelectMany(this, disposable);
                disposable.Disposable = nextObservable.Subscribe(collectionObserver);
            }

            public override void OnError(Exception error)
            {
                lock (this.gate)
                {
                    try {
                        this.observer.OnError(error); } finally {
                        this.Dispose(); };
                }
            }

            public override void OnCompleted()
            {
                this.isStopped = true;
                if (this.collectionDisposable.Count == 1)
                {
                    lock (this.gate)
                    {
                        try {
                            this.observer.OnCompleted(); } finally {
                            this.Dispose(); };
                    }
                }
                else
                {
                    this.sourceDisposable.Dispose();
                }
            }

            class SelectMany : OperatorObserverBase<TResult, TResult>
            {
                readonly SelectManyObserverWithIndex parent;
                readonly IDisposable cancel;

                public SelectMany(SelectManyObserverWithIndex parent, IDisposable cancel)
                    : base(parent.observer, cancel)
                {
                    this.parent = parent;
                    this.cancel = cancel;
                }

                public override void OnNext(TResult value)
                {
                    lock (this.parent.gate)
                    {
                        this.observer.OnNext(value);
                    }
                }

                public override void OnError(Exception error)
                {
                    lock (this.parent.gate)
                    {
                        try {
                            this.observer.OnError(error); } finally {
                            this.Dispose(); };
                    }
                }

                public override void OnCompleted()
                {
                    this.parent.collectionDisposable.Remove(this.cancel);
                    if (this.parent.isStopped && this.parent.collectionDisposable.Count == 1)
                    {
                        lock (this.parent.gate)
                        {
                            try {
                                this.observer.OnCompleted(); } finally {
                                this.Dispose(); };
                        }
                    }
                }
            }
        }

        class SelectManyEnumerableObserver : OperatorObserverBase<TSource, TResult>
        {
            readonly SelectManyObservable<TSource, TResult> parent;

            public SelectManyEnumerableObserver(SelectManyObservable<TSource, TResult> parent, IObserver<TResult> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                return this.parent.source.Subscribe(this);
            }

            public override void OnNext(TSource value)
            {
                IEnumerable<TResult> nextEnumerable;
                try
                {
                    nextEnumerable = this.parent.selectorEnumerable(value);
                }
                catch (Exception ex)
                {
                    try {
                        this.observer.OnError(ex); } finally {
                        this.Dispose(); };
                    return;
                }

                var e = nextEnumerable.GetEnumerator();
                try
                {
                    var hasNext = true;
                    while (hasNext)
                    {
                        hasNext = false;
                        var current = default(TResult);

                        try
                        {
                            hasNext = e.MoveNext();
                            if (hasNext)
                            {
                                current = e.Current;
                            }
                        }
                        catch (Exception exception)
                        {
                            try {
                                this.observer.OnError(exception); } finally {
                                this.Dispose(); }
                            return;
                        }

                        if (hasNext)
                        {
                            this.observer.OnNext(current);
                        }
                    }
                }
                finally
                {
                    if (e != null)
                    {
                        e.Dispose();
                    }
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

        class SelectManyEnumerableObserverWithIndex : OperatorObserverBase<TSource, TResult>
        {
            readonly SelectManyObservable<TSource, TResult> parent;
            int index = 0;

            public SelectManyEnumerableObserverWithIndex(SelectManyObservable<TSource, TResult> parent, IObserver<TResult> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                return this.parent.source.Subscribe(this);
            }

            public override void OnNext(TSource value)
            {
                IEnumerable<TResult> nextEnumerable;
                try
                {
                    nextEnumerable = this.parent.selectorEnumerableWithIndex(value, this.index++);
                }
                catch (Exception ex)
                {
                    this.OnError(ex);
                    return;
                }

                var e = nextEnumerable.GetEnumerator();
                try
                {
                    var hasNext = true;
                    while (hasNext)
                    {
                        hasNext = false;
                        var current = default(TResult);

                        try
                        {
                            hasNext = e.MoveNext();
                            if (hasNext)
                            {
                                current = e.Current;
                            }
                        }
                        catch (Exception exception)
                        {
                            try {
                                this.observer.OnError(exception); } finally {
                                this.Dispose(); }
                            return;
                        }

                        if (hasNext)
                        {
                            this.observer.OnNext(current);
                        }
                    }
                }
                finally
                {
                    if (e != null)
                    {
                        e.Dispose();
                    }
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

    // with resultSelector
    internal class SelectManyObservable<TSource, TCollection, TResult> : OperatorObservableBase<TResult>
    {
        readonly IObservable<TSource> source;
        readonly Func<TSource, IObservable<TCollection>> collectionSelector;
        readonly Func<TSource, int, IObservable<TCollection>> collectionSelectorWithIndex;
        readonly Func<TSource, IEnumerable<TCollection>> collectionSelectorEnumerable;
        readonly Func<TSource, int, IEnumerable<TCollection>> collectionSelectorEnumerableWithIndex;
        readonly Func<TSource, TCollection, TResult> resultSelector;
        readonly Func<TSource, int, TCollection, int, TResult> resultSelectorWithIndex;

        public SelectManyObservable(IObservable<TSource> source, Func<TSource, IObservable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.collectionSelector = collectionSelector;
            this.resultSelector = resultSelector;
        }

        public SelectManyObservable(IObservable<TSource> source, Func<TSource, int, IObservable<TCollection>> collectionSelector, Func<TSource, int, TCollection, int, TResult> resultSelector)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.collectionSelectorWithIndex = collectionSelector;
            this.resultSelectorWithIndex = resultSelector;
        }

        public SelectManyObservable(IObservable<TSource> source, Func<TSource, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.collectionSelectorEnumerable = collectionSelector;
            this.resultSelector = resultSelector;
        }

        public SelectManyObservable(IObservable<TSource> source, Func<TSource, int, IEnumerable<TCollection>> collectionSelector, Func<TSource, int, TCollection, int, TResult> resultSelector)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.collectionSelectorEnumerableWithIndex = collectionSelector;
            this.resultSelectorWithIndex = resultSelector;
        }

        protected override IDisposable SubscribeCore(IObserver<TResult> observer, IDisposable cancel)
        {
            if (this.collectionSelector != null)
            {
                return new SelectManyOuterObserver(this, observer, cancel).Run();
            }
            else if (this.collectionSelectorWithIndex != null)
            {
                return new SelectManyObserverWithIndex(this, observer, cancel).Run();
            }
            else if (this.collectionSelectorEnumerable != null)
            {
                return new SelectManyEnumerableObserver(this, observer, cancel).Run();
            }
            else if (this.collectionSelectorEnumerableWithIndex != null)
            {
                return new SelectManyEnumerableObserverWithIndex(this, observer, cancel).Run();
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        class SelectManyOuterObserver : OperatorObserverBase<TSource, TResult>
        {
            readonly SelectManyObservable<TSource, TCollection, TResult> parent;

            CompositeDisposable collectionDisposable;
            object gate = new object();
            bool isStopped = false;
            SingleAssignmentDisposable sourceDisposable;

            public SelectManyOuterObserver(SelectManyObservable<TSource, TCollection, TResult> parent, IObserver<TResult> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.collectionDisposable = new CompositeDisposable();
                this.sourceDisposable = new SingleAssignmentDisposable();
                this.collectionDisposable.Add(this.sourceDisposable);

                this.sourceDisposable.Disposable = this.parent.source.Subscribe(this);
                return this.collectionDisposable;
            }

            public override void OnNext(TSource value)
            {
                IObservable<TCollection> nextObservable;
                try
                {
                    nextObservable = this.parent.collectionSelector(value);
                }
                catch (Exception ex)
                {
                    this.OnError(ex);
                    return;
                }

                var disposable = new SingleAssignmentDisposable();
                this.collectionDisposable.Add(disposable);
                var collectionObserver = new SelectMany(this, value, disposable);
                disposable.Disposable = nextObservable.Subscribe(collectionObserver);
            }

            public override void OnError(Exception error)
            {
                lock (this.gate)
                {
                    try {
                        this.observer.OnError(error); } finally {
                        this.Dispose(); };
                }
            }

            public override void OnCompleted()
            {
                this.isStopped = true;
                if (this.collectionDisposable.Count == 1)
                {
                    lock (this.gate)
                    {
                        try {
                            this.observer.OnCompleted(); } finally {
                            this.Dispose(); };
                    }
                }
                else
                {
                    this.sourceDisposable.Dispose();
                }
            }

            class SelectMany : OperatorObserverBase<TCollection, TResult>
            {
                readonly SelectManyOuterObserver parent;
                readonly TSource sourceValue;
                readonly IDisposable cancel;

                public SelectMany(SelectManyOuterObserver parent, TSource value, IDisposable cancel)
                    : base(parent.observer, cancel)
                {
                    this.parent = parent;
                    this.sourceValue = value;
                    this.cancel = cancel;
                }

                public override void OnNext(TCollection value)
                {
                    TResult resultValue;
                    try
                    {
                        resultValue = this.parent.parent.resultSelector(this.sourceValue, value);
                    }
                    catch (Exception ex)
                    {
                        this.OnError(ex);
                        return;
                    }

                    lock (this.parent.gate)
                    {
                        this.observer.OnNext(resultValue);
                    }
                }

                public override void OnError(Exception error)
                {
                    lock (this.parent.gate)
                    {
                        try {
                            this.observer.OnError(error); } finally {
                            this.Dispose(); };
                    }
                }

                public override void OnCompleted()
                {
                    this.parent.collectionDisposable.Remove(this.cancel);
                    if (this.parent.isStopped && this.parent.collectionDisposable.Count == 1)
                    {
                        lock (this.parent.gate)
                        {
                            try {
                                this.observer.OnCompleted(); } finally {
                                this.Dispose(); };
                        }
                    }
                }
            }
        }

        class SelectManyObserverWithIndex : OperatorObserverBase<TSource, TResult>
        {
            readonly SelectManyObservable<TSource, TCollection, TResult> parent;

            CompositeDisposable collectionDisposable;
            object gate = new object();
            bool isStopped = false;
            SingleAssignmentDisposable sourceDisposable;
            int index = 0;

            public SelectManyObserverWithIndex(SelectManyObservable<TSource, TCollection, TResult> parent, IObserver<TResult> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.collectionDisposable = new CompositeDisposable();
                this.sourceDisposable = new SingleAssignmentDisposable();
                this.collectionDisposable.Add(this.sourceDisposable);

                this.sourceDisposable.Disposable = this.parent.source.Subscribe(this);
                return this.collectionDisposable;
            }

            public override void OnNext(TSource value)
            {
                var i = this.index++;
                IObservable<TCollection> nextObservable;
                try
                {
                    nextObservable = this.parent.collectionSelectorWithIndex(value, i);
                }
                catch (Exception ex)
                {
                    this.OnError(ex);
                    return;
                }

                var disposable = new SingleAssignmentDisposable();
                this.collectionDisposable.Add(disposable);
                var collectionObserver = new SelectManyObserver(this, value, i, disposable);
                disposable.Disposable = nextObservable.Subscribe(collectionObserver);
            }

            public override void OnError(Exception error)
            {
                lock (this.gate)
                {
                    try {
                        this.observer.OnError(error); } finally {
                        this.Dispose(); };
                }
            }

            public override void OnCompleted()
            {
                this.isStopped = true;
                if (this.collectionDisposable.Count == 1)
                {
                    lock (this.gate)
                    {
                        try {
                            this.observer.OnCompleted(); } finally {
                            this.Dispose(); };
                    }
                }
                else
                {
                    this.sourceDisposable.Dispose();
                }
            }

            class SelectManyObserver : OperatorObserverBase<TCollection, TResult>
            {
                readonly SelectManyObserverWithIndex parent;
                readonly TSource sourceValue;
                readonly int sourceIndex;
                readonly IDisposable cancel;
                int index;

                public SelectManyObserver(SelectManyObserverWithIndex parent, TSource value, int index, IDisposable cancel)
                    : base(parent.observer, cancel)
                {
                    this.parent = parent;
                    this.sourceValue = value;
                    this.sourceIndex = index;
                    this.cancel = cancel;
                }

                public override void OnNext(TCollection value)
                {
                    TResult resultValue;
                    try
                    {
                        resultValue = this.parent.parent.resultSelectorWithIndex(this.sourceValue, this.sourceIndex, value, this.index++);
                    }
                    catch (Exception ex)
                    {
                        try {
                            this.observer.OnError(ex); } finally {
                            this.Dispose(); };
                        return;
                    }

                    lock (this.parent.gate)
                    {
                        this.observer.OnNext(resultValue);
                    }
                }

                public override void OnError(Exception error)
                {
                    lock (this.parent.gate)
                    {
                        try {
                            this.observer.OnError(error); } finally {
                            this.Dispose(); };
                    }
                }

                public override void OnCompleted()
                {
                    this.parent.collectionDisposable.Remove(this.cancel);
                    if (this.parent.isStopped && this.parent.collectionDisposable.Count == 1)
                    {
                        lock (this.parent.gate)
                        {
                            try {
                                this.observer.OnCompleted(); } finally {
                                this.Dispose(); };
                        }
                    }
                }
            }
        }

        class SelectManyEnumerableObserver : OperatorObserverBase<TSource, TResult>
        {
            readonly SelectManyObservable<TSource, TCollection, TResult> parent;

            public SelectManyEnumerableObserver(SelectManyObservable<TSource, TCollection, TResult> parent, IObserver<TResult> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                return this.parent.source.Subscribe(this);
            }

            public override void OnNext(TSource value)
            {
                IEnumerable<TCollection> nextEnumerable;
                try
                {
                    nextEnumerable = this.parent.collectionSelectorEnumerable(value);
                }
                catch (Exception ex)
                {
                    try {
                        this.observer.OnError(ex); } finally {
                        this.Dispose(); };
                    return;
                }

                var e = nextEnumerable.GetEnumerator();
                try
                {
                    var hasNext = true;
                    while (hasNext)
                    {
                        hasNext = false;
                        var current = default(TResult);

                        try
                        {
                            hasNext = e.MoveNext();
                            if (hasNext)
                            {
                                current = this.parent.resultSelector(value, e.Current);
                            }
                        }
                        catch (Exception exception)
                        {
                            try {
                                this.observer.OnError(exception); } finally {
                                this.Dispose(); }
                            return;
                        }

                        if (hasNext)
                        {
                            this.observer.OnNext(current);
                        }
                    }
                }
                finally
                {
                    if (e != null)
                    {
                        e.Dispose();
                    }
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

        class SelectManyEnumerableObserverWithIndex : OperatorObserverBase<TSource, TResult>
        {
            readonly SelectManyObservable<TSource, TCollection, TResult> parent;
            int index = 0;

            public SelectManyEnumerableObserverWithIndex(SelectManyObservable<TSource, TCollection, TResult> parent, IObserver<TResult> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                return this.parent.source.Subscribe(this);
            }

            public override void OnNext(TSource value)
            {
                var i = this.index++;
                IEnumerable<TCollection> nextEnumerable;
                try
                {
                    nextEnumerable = this.parent.collectionSelectorEnumerableWithIndex(value, i);
                }
                catch (Exception ex)
                {
                    try {
                        this.observer.OnError(ex); } finally {
                        this.Dispose(); };
                    return;
                }

                var e = nextEnumerable.GetEnumerator();
                try
                {
                    var sequenceI = 0;
                    var hasNext = true;
                    while (hasNext)
                    {
                        hasNext = false;
                        var current = default(TResult);

                        try
                        {
                            hasNext = e.MoveNext();
                            if (hasNext)
                            {
                                current = this.parent.resultSelectorWithIndex(value, i, e.Current, sequenceI++);
                            }
                        }
                        catch (Exception exception)
                        {
                            try {
                                this.observer.OnError(exception); } finally {
                                this.Dispose(); }
                            return;
                        }

                        if (hasNext)
                        {
                            this.observer.OnNext(current);
                        }
                    }
                }
                finally
                {
                    if (e != null)
                    {
                        e.Dispose();
                    }
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