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
    internal class ThrottleFrameObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly int frameCount;
        readonly FrameCountType frameCountType;

        public ThrottleFrameObservable(IObservable<T> source, int frameCount, FrameCountType frameCountType) : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.frameCount = frameCount;
            this.frameCountType = frameCountType;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return new ThrottleFrame(this, observer, cancel).Run();
        }

        class ThrottleFrame : OperatorObserverBase<T, T>
        {
            readonly ThrottleFrameObservable<T> parent;
            readonly object gate = new object();
            T latestValue = default(T);
            bool hasValue = false;
            SerialDisposable cancelable;
            ulong id = 0;

            public ThrottleFrame(ThrottleFrameObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.cancelable = new SerialDisposable();
                var subscription = this.parent.source.Subscribe(this);

                return StableCompositeDisposable.Create(this.cancelable, subscription);
            }

            public override void OnNext(T value)
            {
                ulong currentid;
                lock (this.gate)
                {
                    this.hasValue = true;
                    this.latestValue = value;
                    this.id = unchecked(this.id + 1);
                    currentid = this.id;
                }

                var d = new SingleAssignmentDisposable();
                this.cancelable.Disposable = d;
                d.Disposable = Observable.TimerFrame(this.parent.frameCount, this.parent.frameCountType)
                    .Subscribe(new ThrottleFrameTick(this, currentid));
            }

            public override void OnError(Exception error)
            {
                this.cancelable.Dispose();

                lock (this.gate)
                {
                    this.hasValue = false;
                    this.id = unchecked(this.id + 1);
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
                    if (this.hasValue)
                    {
                        this.observer.OnNext(this.latestValue);
                    }

                    this.hasValue = false;
                    this.id = unchecked(this.id + 1);
                    try {
                        this.observer.OnCompleted(); } finally {
                        this.Dispose(); }
                }
            }

            class ThrottleFrameTick : IObserver<long>
            {
                readonly ThrottleFrame parent;
                readonly ulong currentid;

                public ThrottleFrameTick(ThrottleFrame parent, ulong currentid)
                {
                    this.parent = parent;
                    this.currentid = currentid;
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
                        if (this.parent.hasValue && this.parent.id == this.currentid)
                        {
                            this.parent.observer.OnNext(this.parent.latestValue);
                        }

                        this.parent.hasValue = false;
                    }
                }
            }
        }
    }
}