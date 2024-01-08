using System;
using System.Collections.Generic;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class MergeObservable<T> : OperatorObservableBase<T>
    {
        private readonly IObservable<IObservable<T>> sources;
        private readonly int maxConcurrent;

        public MergeObservable(IObservable<IObservable<T>> sources, bool isRequiredSubscribeOnCurrentThread)
            : base(isRequiredSubscribeOnCurrentThread)
        {
            this.sources = sources;
        }

        public MergeObservable(IObservable<IObservable<T>> sources, int maxConcurrent, bool isRequiredSubscribeOnCurrentThread)
            : base(isRequiredSubscribeOnCurrentThread)
        {
            this.sources = sources;
            this.maxConcurrent = maxConcurrent;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            if (this.maxConcurrent > 0)
            {
                return new MergeConcurrentObserver(this, observer, cancel).Run();
            }
            else
            {
                return new MergeOuterObserver(this, observer, cancel).Run();
            }
        }

        class MergeOuterObserver : OperatorObserverBase<IObservable<T>, T>
        {
            readonly MergeObservable<T> parent;

            CompositeDisposable collectionDisposable;
            SingleAssignmentDisposable sourceDisposable;
            object gate = new object();
            bool isStopped = false;

            public MergeOuterObserver(MergeObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.collectionDisposable = new CompositeDisposable();
                this.sourceDisposable = new SingleAssignmentDisposable();
                this.collectionDisposable.Add(this.sourceDisposable);

                this.sourceDisposable.Disposable = this.parent.sources.Subscribe(this);
                return this.collectionDisposable;
            }

            public override void OnNext(IObservable<T> value)
            {
                var disposable = new SingleAssignmentDisposable();
                this.collectionDisposable.Add(disposable);
                var collectionObserver = new Merge(this, disposable);
                disposable.Disposable = value.Subscribe(collectionObserver);
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

            class Merge : OperatorObserverBase<T, T>
            {
                readonly MergeOuterObserver parent;
                readonly IDisposable cancel;

                public Merge(MergeOuterObserver parent, IDisposable cancel)
                    : base(parent.observer, cancel)
                {
                    this.parent = parent;
                    this.cancel = cancel;
                }

                public override void OnNext(T value)
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

        class MergeConcurrentObserver : OperatorObserverBase<IObservable<T>, T>
        {
            readonly MergeObservable<T> parent;

            CompositeDisposable collectionDisposable;
            SingleAssignmentDisposable sourceDisposable;
            object gate = new object();
            bool isStopped = false;

            // concurrency
            Queue<IObservable<T>> q;
            int activeCount;

            public MergeConcurrentObserver(MergeObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.q = new Queue<IObservable<T>>();
                this.activeCount = 0;

                this.collectionDisposable = new CompositeDisposable();
                this.sourceDisposable = new SingleAssignmentDisposable();
                this.collectionDisposable.Add(this.sourceDisposable);

                this.sourceDisposable.Disposable = this.parent.sources.Subscribe(this);
                return this.collectionDisposable;
            }

            public override void OnNext(IObservable<T> value)
            {
                lock (this.gate)
                {
                    if (this.activeCount < this.parent.maxConcurrent)
                    {
                        this.activeCount++;
                        this.Subscribe(value);
                    }
                    else
                    {
                        this.q.Enqueue(value);
                    }
                }
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
                lock (this.gate)
                {
                    this.isStopped = true;
                    if (this.activeCount == 0)
                    {
                        try {
                            this.observer.OnCompleted(); } finally {
                            this.Dispose(); };
                    }
                    else
                    {
                        this.sourceDisposable.Dispose();
                    }
                }
            }

            void Subscribe(IObservable<T> innerSource)
            {
                var disposable = new SingleAssignmentDisposable();
                this.collectionDisposable.Add(disposable);
                var collectionObserver = new Merge(this, disposable);
                disposable.Disposable = innerSource.Subscribe(collectionObserver);
            }

            class Merge : OperatorObserverBase<T, T>
            {
                readonly MergeConcurrentObserver parent;
                readonly IDisposable cancel;

                public Merge(MergeConcurrentObserver parent, IDisposable cancel)
                    : base(parent.observer, cancel)
                {
                    this.parent = parent;
                    this.cancel = cancel;
                }

                public override void OnNext(T value)
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
                    lock (this.parent.gate)
                    {
                        if (this.parent.q.Count > 0)
                        {
                            var source = this.parent.q.Dequeue();
                            this.parent.Subscribe(source);
                        }
                        else
                        {
                            this.parent.activeCount--;
                            if (this.parent.isStopped && this.parent.activeCount == 0)
                            {
                                try {
                                    this.observer.OnCompleted(); } finally {
                                    this.Dispose(); };
                            }
                        }
                    }
                }
            }
        }
    }
}