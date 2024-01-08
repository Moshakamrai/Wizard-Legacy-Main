using System;
using System.Collections;
using System.Collections.Generic;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.UnityEngineBridge.Operators
{
    internal class DelayFrameObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly int frameCount;
        readonly FrameCountType frameCountType;

        public DelayFrameObservable(IObservable<T> source, int frameCount, FrameCountType frameCountType)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.frameCount = frameCount;
            this.frameCountType = frameCountType;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return new DelayFrame(this, observer, cancel).Run();
        }

        class DelayFrame : OperatorObserverBase<T, T>
        {
            readonly DelayFrameObservable<T> parent;
            readonly object gate = new object();
            readonly QueuePool pool = new QueuePool();
            int runningEnumeratorCount;
            bool readyDrainEnumerator;
            bool running;
            IDisposable sourceSubscription;
            Queue<T> currentQueueReference;
            bool calledCompleted;
            bool hasError;
            Exception error;
            BooleanDisposable cancelationToken;

            public DelayFrame(DelayFrameObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.cancelationToken = new BooleanDisposable();

                var _sourceSubscription = new SingleAssignmentDisposable();
                this.sourceSubscription = _sourceSubscription;
                _sourceSubscription.Disposable = this.parent.source.Subscribe(this);

                return StableCompositeDisposable.Create(this.cancelationToken, this.sourceSubscription);
            }

            IEnumerator DrainQueue(Queue<T> q, int frameCount)
            {
                lock (this.gate)
                {
                    this.readyDrainEnumerator = false; // use next queue.
                    this.running = false;
                }

                while (!this.cancelationToken.IsDisposed && frameCount-- != 0)
                {
                    yield return null;
                }

                try
                {
                    if (q != null)
                    {
                        while (q.Count > 0 && !this.hasError)
                        {
                            if (this.cancelationToken.IsDisposed) break;

                            lock (this.gate)
                            {
                                this.running = true;
                            }

                            var value = q.Dequeue();
                            this.observer.OnNext(value);

                            lock (this.gate)
                            {
                                this.running = false;
                            }
                        }

                        if (q.Count == 0)
                        {
                            this.pool.Return(q);
                        }
                    }

                    if (this.hasError)
                    {
                        if (!this.cancelationToken.IsDisposed)
                        {
                            this.cancelationToken.Dispose();

                            try {
                                this.observer.OnError(this.error); } finally {
                                this.Dispose(); }
                        }
                    }
                    else if (this.calledCompleted)
                    {
                        lock (this.gate)
                        {
                            // not self only
                            if (this.runningEnumeratorCount != 1) yield break;
                        }

                        if (!this.cancelationToken.IsDisposed)
                        {
                            this.cancelationToken.Dispose();

                            try {
                                this.observer.OnCompleted(); }
                            finally {
                                this.Dispose(); }
                        }
                    }
                }
                finally
                {
                    lock (this.gate)
                    {
                        this.runningEnumeratorCount--;
                    }
                }
            }

            public override void OnNext(T value)
            {
                if (this.cancelationToken.IsDisposed) return;

                Queue<T> targetQueue = null;
                lock (this.gate)
                {
                    if (!this.readyDrainEnumerator)
                    {
                        this.readyDrainEnumerator = true;
                        this.runningEnumeratorCount++;
                        targetQueue = this.currentQueueReference = this.pool.Get();
                        targetQueue.Enqueue(value);
                    }
                    else
                    {
                        if (this.currentQueueReference != null) // null - if doesn't start OnNext and start OnCompleted
                        {
                            this.currentQueueReference.Enqueue(value);
                        }
                        return;
                    }
                }

                switch (this.parent.frameCountType)
                {
                    case FrameCountType.Update:
                        MainThreadDispatcher.StartUpdateMicroCoroutine(this.DrainQueue(targetQueue, this.parent.frameCount));
                        break;
                    case FrameCountType.FixedUpdate:
                        MainThreadDispatcher.StartFixedUpdateMicroCoroutine(this.DrainQueue(targetQueue, this.parent.frameCount));
                        break;
                    case FrameCountType.EndOfFrame:
                        MainThreadDispatcher.StartEndOfFrameMicroCoroutine(this.DrainQueue(targetQueue, this.parent.frameCount));
                        break;
                    default:
                        throw new ArgumentException("Invalid FrameCountType:" + this.parent.frameCountType);
                }
            }

            public override void OnError(Exception error)
            {
                this.sourceSubscription.Dispose(); // stop subscription

                if (this.cancelationToken.IsDisposed) return;

                lock (this.gate)
                {
                    if (this.running)
                    {
                        this.hasError = true;
                        this.error = error;
                        return;
                    }
                }

                this.cancelationToken.Dispose();
                try { this.observer.OnError(error); } finally {
                    this.Dispose(); }
            }

            public override void OnCompleted()
            {
                this.sourceSubscription.Dispose(); // stop subscription

                if (this.cancelationToken.IsDisposed) return;

                lock (this.gate)
                {
                    this.calledCompleted = true;

                    if (!this.readyDrainEnumerator)
                    {
                        this.readyDrainEnumerator = true;
                        this.runningEnumeratorCount++;
                    }
                    else
                    {
                        return;
                    }
                }

                switch (this.parent.frameCountType)
                {
                    case FrameCountType.Update:
                        MainThreadDispatcher.StartUpdateMicroCoroutine(this.DrainQueue(null, this.parent.frameCount));
                        break;
                    case FrameCountType.FixedUpdate:
                        MainThreadDispatcher.StartFixedUpdateMicroCoroutine(this.DrainQueue(null, this.parent.frameCount));
                        break;
                    case FrameCountType.EndOfFrame:
                        MainThreadDispatcher.StartEndOfFrameMicroCoroutine(this.DrainQueue(null, this.parent.frameCount));
                        break;
                    default:
                        throw new ArgumentException("Invalid FrameCountType:" + this.parent.frameCountType);
                }
            }
        }

        class QueuePool
        {
            readonly object gate = new object();
            readonly Queue<Queue<T>> pool = new Queue<Queue<T>>(2);

            public Queue<T> Get()
            {
                lock (this.gate)
                {
                    if (this.pool.Count == 0)
                    {
                        return new Queue<T>(2);
                    }
                    else
                    {
                        return this.pool.Dequeue();
                    }
                }
            }

            public void Return(Queue<T> q)
            {
                lock (this.gate)
                {
                    this.pool.Enqueue(q);
                }
            }
        }
    }
}