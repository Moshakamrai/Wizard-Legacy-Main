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
    internal class ThrottleFirstFrameObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly int frameCount;
        readonly FrameCountType frameCountType;

        public ThrottleFirstFrameObservable(IObservable<T> source, int frameCount, FrameCountType frameCountType) : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.frameCount = frameCount;
            this.frameCountType = frameCountType;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return new ThrottleFirstFrame(this, observer, cancel).Run();
        }

        class ThrottleFirstFrame : OperatorObserverBase<T, T>
        {
            readonly ThrottleFirstFrameObservable<T> parent;
            readonly object gate = new object();
            bool open = true;
            SerialDisposable cancelable;

            ThrottleFirstFrameTick tick;

            public ThrottleFirstFrame(ThrottleFirstFrameObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.tick = new ThrottleFirstFrameTick(this);
                this.cancelable = new SerialDisposable();

                var subscription = this.parent.source.Subscribe(this);
                return StableCompositeDisposable.Create(this.cancelable, subscription);
            }

            void OnNext()
            {
                lock (this.gate)
                {
                    this.open = true;
                }
            }

            public override void OnNext(T value)
            {
                lock (this.gate)
                {
                    if (!this.open) return;
                    this.observer.OnNext(value);
                    this.open = false;
                }

                var d = new SingleAssignmentDisposable();
                this.cancelable.Disposable = d;
                d.Disposable = Observable.TimerFrame(this.parent.frameCount, this.parent.frameCountType)
                    .Subscribe(this.tick);
            }

            public override void OnError(Exception error)
            {
                this.cancelable.Dispose();

                lock (this.gate)
                {
                    try {
                        this.observer.OnError(error); } finally {
                        this.Dispose(); }
                }
            }

            public override void OnCompleted()
            {
                this.cancelable.Dispose();

                lock (this.gate)
                {
                    try {
                        this.observer.OnCompleted(); } finally {
                        this.Dispose(); }
                }
            }

            // immutable, can share.
            class ThrottleFirstFrameTick : IObserver<long>
            {
                readonly ThrottleFirstFrame parent;

                public ThrottleFirstFrameTick(ThrottleFirstFrame parent)
                {
                    this.parent = parent;
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
                        this.parent.open = true;
                    }
                }
            }
        }
    }
}