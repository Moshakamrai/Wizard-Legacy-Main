﻿using System;
using System.Collections.Generic;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class WhenAllObservable<T> : OperatorObservableBase<T[]>
    {
        readonly IObservable<T>[] sources;
        readonly IEnumerable<IObservable<T>> sourcesEnumerable;

        public WhenAllObservable(IObservable<T>[] sources)
            : base(false)
        {
            this.sources = sources;
        }

        public WhenAllObservable(IEnumerable<IObservable<T>> sources)
            : base(false)
        {
            this.sourcesEnumerable = sources;
        }

        protected override IDisposable SubscribeCore(IObserver<T[]> observer, IDisposable cancel)
        {
            if (this.sources != null)
            {
                return new WhenAll(this.sources, observer, cancel).Run();
            }
            else
            {
                var xs = this.sourcesEnumerable as IList<IObservable<T>>;
                if (xs == null)
                {
                    xs = new List<IObservable<T>>(this.sourcesEnumerable); // materialize observables
                }
                return new WhenAll_(xs, observer, cancel).Run();
            }
        }

        class WhenAll : OperatorObserverBase<T[], T[]>
        {
            readonly IObservable<T>[] sources;
            readonly object gate = new object();
            int completedCount;
            int length;
            T[] values;

            public WhenAll(IObservable<T>[] sources, IObserver<T[]> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                this.sources = sources;
            }

            public IDisposable Run()
            {
                this.length = this.sources.Length;

                // fail safe...
                if (this.length == 0)
                {
                    this.OnNext(new T[0]);
                    try {
                        this.observer.OnCompleted(); } finally {
                        this.Dispose(); }
                    return Disposable.Empty;
                }

                this.completedCount = 0;
                this.values = new T[this.length];

                var subscriptions = new IDisposable[this.length];
                for (int index = 0; index < this.length; index++)
                {
                    var source = this.sources[index];
                    var observer = new WhenAllCollectionObserver(this, index);
                    subscriptions[index] = source.Subscribe(observer);
                }

                return StableCompositeDisposable.CreateUnsafe(subscriptions);
            }

            public override void OnNext(T[] value)
            {
                this.observer.OnNext(value);
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

            class WhenAllCollectionObserver : IObserver<T>
            {
                readonly WhenAll parent;
                readonly int index;
                bool isCompleted = false;

                public WhenAllCollectionObserver(WhenAll parent, int index)
                {
                    this.parent = parent;
                    this.index = index;
                }

                public void OnNext(T value)
                {
                    lock (this.parent.gate)
                    {
                        if (!this.isCompleted)
                        {
                            this.parent.values[this.index] = value;
                        }
                    }
                }

                public void OnError(Exception error)
                {
                    lock (this.parent.gate)
                    {
                        if (!this.isCompleted)
                        {
                            this.parent.OnError(error);
                        }
                    }
                }

                public void OnCompleted()
                {
                    lock (this.parent.gate)
                    {
                        if (!this.isCompleted)
                        {
                            this.isCompleted = true;
                            this.parent.completedCount++;
                            if (this.parent.completedCount == this.parent.length)
                            {
                                this.parent.OnNext(this.parent.values);
                                this.parent.OnCompleted();
                            }
                        }
                    }
                }
            }
        }

        class WhenAll_ : OperatorObserverBase<T[], T[]>
        {
            readonly IList<IObservable<T>> sources;
            readonly object gate = new object();
            int completedCount;
            int length;
            T[] values;

            public WhenAll_(IList<IObservable<T>> sources, IObserver<T[]> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                this.sources = sources;
            }

            public IDisposable Run()
            {
                this.length = this.sources.Count;

                // fail safe...
                if (this.length == 0)
                {
                    this.OnNext(new T[0]);
                    try {
                        this.observer.OnCompleted(); } finally {
                        this.Dispose(); }
                    return Disposable.Empty;
                }

                this.completedCount = 0;
                this.values = new T[this.length];

                var subscriptions = new IDisposable[this.length];
                for (int index = 0; index < this.length; index++)
                {
                    var source = this.sources[index];
                    var observer = new WhenAllCollectionObserver(this, index);
                    subscriptions[index] = source.Subscribe(observer);
                }

                return StableCompositeDisposable.CreateUnsafe(subscriptions);
            }

            public override void OnNext(T[] value)
            {
                this.observer.OnNext(value);
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

            class WhenAllCollectionObserver : IObserver<T>
            {
                readonly WhenAll_ parent;
                readonly int index;
                bool isCompleted = false;

                public WhenAllCollectionObserver(WhenAll_ parent, int index)
                {
                    this.parent = parent;
                    this.index = index;
                }

                public void OnNext(T value)
                {
                    lock (this.parent.gate)
                    {
                        if (!this.isCompleted)
                        {
                            this.parent.values[this.index] = value;
                        }
                    }
                }

                public void OnError(Exception error)
                {
                    lock (this.parent.gate)
                    {
                        if (!this.isCompleted)
                        {
                            this.parent.OnError(error);
                        }
                    }
                }

                public void OnCompleted()
                {
                    lock (this.parent.gate)
                    {
                        if (!this.isCompleted)
                        {
                            this.isCompleted = true;
                            this.parent.completedCount++;
                            if (this.parent.completedCount == this.parent.length)
                            {
                                this.parent.OnNext(this.parent.values);
                                this.parent.OnCompleted();
                            }
                        }
                    }
                }
            }
        }
    }

    internal class WhenAllObservable : OperatorObservableBase<Unit>
    {
        readonly IObservable<Unit>[] sources;
        readonly IEnumerable<IObservable<Unit>> sourcesEnumerable;

        public WhenAllObservable(IObservable<Unit>[] sources)
            : base(false)
        {
            this.sources = sources;
        }

        public WhenAllObservable(IEnumerable<IObservable<Unit>> sources)
            : base(false)
        {
            this.sourcesEnumerable = sources;
        }

        protected override IDisposable SubscribeCore(IObserver<Unit> observer, IDisposable cancel)
        {
            if (this.sources != null)
            {
                return new WhenAll(this.sources, observer, cancel).Run();
            }
            else
            {
                var xs = this.sourcesEnumerable as IList<IObservable<Unit>>;
                if (xs == null)
                {
                    xs = new List<IObservable<Unit>>(this.sourcesEnumerable); // materialize observables
                }
                return new WhenAll_(xs, observer, cancel).Run();
            }
        }

        class WhenAll : OperatorObserverBase<Unit, Unit>
        {
            readonly IObservable<Unit>[] sources;
            readonly object gate = new object();
            int completedCount;
            int length;

            public WhenAll(IObservable<Unit>[] sources, IObserver<Unit> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                this.sources = sources;
            }

            public IDisposable Run()
            {
                this.length = this.sources.Length;

                // fail safe...
                if (this.length == 0)
                {
                    this.OnNext(Unit.Default);
                    try {
                        this.observer.OnCompleted(); } finally {
                        this.Dispose(); }
                    return Disposable.Empty;
                }

                this.completedCount = 0;

                var subscriptions = new IDisposable[this.length];
                for (int index = 0; index < this.sources.Length; index++)
                {
                    var source = this.sources[index];
                    var observer = new WhenAllCollectionObserver(this);
                    subscriptions[index] = source.Subscribe(observer);
                }

                return StableCompositeDisposable.CreateUnsafe(subscriptions);
            }

            public override void OnNext(Unit value)
            {
                this.observer.OnNext(value);
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

            class WhenAllCollectionObserver : IObserver<Unit>
            {
                readonly WhenAll parent;
                bool isCompleted = false;

                public WhenAllCollectionObserver(WhenAll parent)
                {
                    this.parent = parent;
                }

                public void OnNext(Unit value)
                {
                }

                public void OnError(Exception error)
                {
                    lock (this.parent.gate)
                    {
                        if (!this.isCompleted)
                        {
                            this.parent.OnError(error);
                        }
                    }
                }

                public void OnCompleted()
                {
                    lock (this.parent.gate)
                    {
                        if (!this.isCompleted)
                        {
                            this.isCompleted = true;
                            this.parent.completedCount++;
                            if (this.parent.completedCount == this.parent.length)
                            {
                                this.parent.OnNext(Unit.Default);
                                this.parent.OnCompleted();
                            }
                        }
                    }
                }
            }
        }

        class WhenAll_ : OperatorObserverBase<Unit, Unit>
        {
            readonly IList<IObservable<Unit>> sources;
            readonly object gate = new object();
            int completedCount;
            int length;

            public WhenAll_(IList<IObservable<Unit>> sources, IObserver<Unit> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                this.sources = sources;
            }

            public IDisposable Run()
            {
                this.length = this.sources.Count;

                // fail safe...
                if (this.length == 0)
                {
                    this.OnNext(Unit.Default);
                    try {
                        this.observer.OnCompleted(); } finally {
                        this.Dispose(); }
                    return Disposable.Empty;
                }

                this.completedCount = 0;

                var subscriptions = new IDisposable[this.length];
                for (int index = 0; index < this.length; index++)
                {
                    var source = this.sources[index];
                    var observer = new WhenAllCollectionObserver(this);
                    subscriptions[index] = source.Subscribe(observer);
                }

                return StableCompositeDisposable.CreateUnsafe(subscriptions);
            }

            public override void OnNext(Unit value)
            {
                this.observer.OnNext(value);
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

            class WhenAllCollectionObserver : IObserver<Unit>
            {
                readonly WhenAll_ parent;
                bool isCompleted = false;

                public WhenAllCollectionObserver(WhenAll_ parent)
                {
                    this.parent = parent;
                }

                public void OnNext(Unit value)
                {
                }

                public void OnError(Exception error)
                {
                    lock (this.parent.gate)
                    {
                        if (!this.isCompleted)
                        {
                            this.parent.OnError(error);
                        }
                    }
                }

                public void OnCompleted()
                {
                    lock (this.parent.gate)
                    {
                        if (!this.isCompleted)
                        {
                            this.isCompleted = true;
                            this.parent.completedCount++;
                            if (this.parent.completedCount == this.parent.length)
                            {
                                this.parent.OnNext(Unit.Default);
                                this.parent.OnCompleted();
                            }
                        }
                    }
                }
            }
        }
    }
}