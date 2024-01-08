#if UniRxLibrary
using UnityObservable = UniRx.ObservableUnity;
#else
#endif
using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.UnityEngineBridge.Operators
{
    internal class TimeoutFrameObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly int frameCount;
        readonly FrameCountType frameCountType;

        public TimeoutFrameObservable(IObservable<T> source, int frameCount, FrameCountType frameCountType) : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.frameCount = frameCount;
            this.frameCountType = frameCountType;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return new TimeoutFrame(this, observer, cancel).Run();
        }

        class TimeoutFrame : OperatorObserverBase<T, T>
        {
            readonly TimeoutFrameObservable<T> parent;
            readonly object gate = new object();
            ulong objectId = 0ul;
            bool isTimeout = false;
            SingleAssignmentDisposable sourceSubscription;
            SerialDisposable timerSubscription;

            public TimeoutFrame(TimeoutFrameObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.sourceSubscription = new SingleAssignmentDisposable();
                this.timerSubscription = new SerialDisposable();
                this.timerSubscription.Disposable = this.RunTimer(this.objectId);
                this.sourceSubscription.Disposable = this.parent.source.Subscribe(this);

                return StableCompositeDisposable.Create(this.timerSubscription, this.sourceSubscription);
            }

            IDisposable RunTimer(ulong timerId)
            {
                return Observable.TimerFrame(this.parent.frameCount, this.parent.frameCountType)
                    .Subscribe(new TimeoutFrameTick(this, timerId));
            }

            public override void OnNext(T value)
            {
                ulong useObjectId;
                bool timeout;
                lock (this.gate)
                {
                    timeout = this.isTimeout;
                    this.objectId++;
                    useObjectId = this.objectId;
                }
                if (timeout) return;

                this.timerSubscription.Disposable = Disposable.Empty; // cancel old timer
                this.observer.OnNext(value);
                this.timerSubscription.Disposable = this.RunTimer(useObjectId);
            }

            public override void OnError(Exception error)
            {
                bool timeout;
                lock (this.gate)
                {
                    timeout = this.isTimeout;
                    this.objectId++;
                }
                if (timeout) return;

                this.timerSubscription.Dispose();
                try {
                    this.observer.OnError(error); } finally {
                    this.Dispose(); }
            }

            public override void OnCompleted()
            {
                bool timeout;
                lock (this.gate)
                {
                    timeout = this.isTimeout;
                    this.objectId++;
                }
                if (timeout) return;

                this.timerSubscription.Dispose();
                try {
                    this.observer.OnCompleted(); } finally {
                    this.Dispose(); }
            }

            class TimeoutFrameTick : IObserver<long>
            {
                readonly TimeoutFrame parent;
                readonly ulong timerId;

                public TimeoutFrameTick(TimeoutFrame parent, ulong timerId)
                {
                    this.parent = parent;
                    this.timerId = timerId;
                }

                public void OnCompleted()
                {
                }

                public void OnError(Exception error)
                {
                }

                public void OnNext(long _)
                {


                    lock (this.parent.gate)
                    {
                        if (this.parent.objectId == this.timerId)
                        {
                            this.parent.isTimeout = true;
                        }
                    }
                    if (this.parent.isTimeout)
                    {
                        try {
                            this.parent.observer.OnError(new TimeoutException()); } finally {
                            this.parent.Dispose(); }
                    }
                }
            }
        }
    }
}