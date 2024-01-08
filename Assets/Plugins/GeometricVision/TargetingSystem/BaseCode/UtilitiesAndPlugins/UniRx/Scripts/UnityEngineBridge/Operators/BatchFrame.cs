using System;
using System.Collections;
using System.Collections.Generic;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.UnityEngineBridge.Operators
{
    internal class BatchFrameObservable<T> : OperatorObservableBase<IList<T>>
    {
        readonly IObservable<T> source;
        readonly int frameCount;
        readonly FrameCountType frameCountType;

        public BatchFrameObservable(IObservable<T> source, int frameCount, FrameCountType frameCountType)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.frameCount = frameCount;
            this.frameCountType = frameCountType;
        }

        protected override IDisposable SubscribeCore(IObserver<IList<T>> observer, IDisposable cancel)
        {
            return new BatchFrame(this, observer, cancel).Run();
        }

        class BatchFrame : OperatorObserverBase<T, IList<T>>
        {
            readonly BatchFrameObservable<T> parent;
            readonly object gate = new object();
            readonly BooleanDisposable cancellationToken = new BooleanDisposable();
            readonly IEnumerator timer;
            bool isRunning;
            bool isCompleted;
            List<T> list;

            public BatchFrame(BatchFrameObservable<T> parent, IObserver<IList<T>> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
                this.timer = new ReusableEnumerator(this);
            }

            public IDisposable Run()
            {
                this.list = new List<T>();
                var sourceSubscription = this.parent.source.Subscribe(this);
                return StableCompositeDisposable.Create(sourceSubscription, this.cancellationToken);
            }

            public override void OnNext(T value)
            {
                lock (this.gate)
                {
                    if (this.isCompleted) return;
                    this.list.Add(value);
                    if (!this.isRunning)
                    {
                        this.isRunning = true;
                        this.timer.Reset(); // reuse

                        switch (this.parent.frameCountType)
                        {
                            case FrameCountType.Update:
                                MainThreadDispatcher.StartUpdateMicroCoroutine(this.timer);
                                break;
                            case FrameCountType.FixedUpdate:
                                MainThreadDispatcher.StartFixedUpdateMicroCoroutine(this.timer);
                                break;
                            case FrameCountType.EndOfFrame:
                                MainThreadDispatcher.StartEndOfFrameMicroCoroutine(this.timer);
                                break;
                            default:
                                break;
                        }
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
                List<T> currentList;
                lock (this.gate)
                {
                    this.isCompleted = true;
                    currentList = this.list;
                }
                if (currentList.Count != 0)
                {
                    this.observer.OnNext(currentList);
                }
                try {
                    this.observer.OnCompleted(); } finally {
                    this.Dispose(); }
            }

            // reuse, no gc allocate
            class ReusableEnumerator : IEnumerator
            {
                readonly BatchFrame parent;
                int currentFrame;

                public ReusableEnumerator(BatchFrame parent)
                {
                    this.parent = parent;
                }

                public object Current
                {
                    get { return null; }
                }

                public bool MoveNext()
                {
                    if (this.parent.cancellationToken.IsDisposed) return false;

                    List<T> currentList;
                    lock (this.parent.gate)
                    {
                        if (this.currentFrame++ == this.parent.parent.frameCount)
                        {
                            if (this.parent.isCompleted) return false;

                            currentList = this.parent.list;
                            this.parent.list = new List<T>();
                            this.parent.isRunning = false;

                            // exit lock 
                        }
                        else
                        {
                            return true;
                        }
                    }

                    this.parent.observer.OnNext(currentList);
                    return false;
                }

                public void Reset()
                {
                    this.currentFrame = 0;
                }
            }
        }
    }

    internal class BatchFrameObservable : OperatorObservableBase<Unit>
    {
        readonly IObservable<Unit> source;
        readonly int frameCount;
        readonly FrameCountType frameCountType;

        public BatchFrameObservable(IObservable<Unit> source, int frameCount, FrameCountType frameCountType)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.frameCount = frameCount;
            this.frameCountType = frameCountType;
        }

        protected override IDisposable SubscribeCore(IObserver<Unit> observer, IDisposable cancel)
        {
            return new BatchFrame(this, observer, cancel).Run();
        }

        class BatchFrame : OperatorObserverBase<Unit, Unit>
        {
            readonly BatchFrameObservable parent;
            readonly object gate = new object();
            readonly BooleanDisposable cancellationToken = new BooleanDisposable();
            readonly IEnumerator timer;

            bool isRunning;
            bool isCompleted;

            public BatchFrame(BatchFrameObservable parent, IObserver<Unit> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
                this.timer = new ReusableEnumerator(this);
            }

            public IDisposable Run()
            {
                var sourceSubscription = this.parent.source.Subscribe(this);
                return StableCompositeDisposable.Create(sourceSubscription, this.cancellationToken);
            }

            public override void OnNext(Unit value)
            {
                lock (this.gate)
                {
                    if (!this.isRunning)
                    {
                        this.isRunning = true;
                        this.timer.Reset(); // reuse

                        switch (this.parent.frameCountType)
                        {
                            case FrameCountType.Update:
                                MainThreadDispatcher.StartUpdateMicroCoroutine(this.timer);
                                break;
                            case FrameCountType.FixedUpdate:
                                MainThreadDispatcher.StartFixedUpdateMicroCoroutine(this.timer);
                                break;
                            case FrameCountType.EndOfFrame:
                                MainThreadDispatcher.StartEndOfFrameMicroCoroutine(this.timer);
                                break;
                            default:
                                break;
                        }
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
                bool running;
                lock (this.gate)
                {
                    running = this.isRunning;
                    this.isCompleted = true;
                }
                if (running)
                {
                    this.observer.OnNext(Unit.Default);
                }
                try {
                    this.observer.OnCompleted(); } finally {
                    this.Dispose(); }
            }

            // reuse, no gc allocate
            class ReusableEnumerator : IEnumerator
            {
                readonly BatchFrame parent;
                int currentFrame;

                public ReusableEnumerator(BatchFrame parent)
                {
                    this.parent = parent;
                }

                public object Current
                {
                    get { return null; }
                }

                public bool MoveNext()
                {
                    if (this.parent.cancellationToken.IsDisposed) return false;

                    lock (this.parent.gate)
                    {
                        if (this.currentFrame++ == this.parent.parent.frameCount)
                        {
                            if (this.parent.isCompleted) return false;
                            this.parent.isRunning = false;

                            // exit lock 
                        }
                        else
                        {
                            return true;
                        }
                    }

                    this.parent.observer.OnNext(Unit.Default);
                    return false;
                }

                public void Reset()
                {
                    this.currentFrame = 0;
                }
            }
        }
    }
}