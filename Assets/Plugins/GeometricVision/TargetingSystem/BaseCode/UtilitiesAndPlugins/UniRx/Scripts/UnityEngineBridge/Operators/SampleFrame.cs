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
    internal class SampleFrameObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly int frameCount;
        readonly FrameCountType frameCountType;

        public SampleFrameObservable(IObservable<T> source, int frameCount, FrameCountType frameCountType) : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.frameCount = frameCount;
            this.frameCountType = frameCountType;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return new SampleFrame(this, observer, cancel).Run();
        }

        class SampleFrame : OperatorObserverBase<T, T>
        {
            readonly SampleFrameObservable<T> parent;
            readonly object gate = new object();
            T latestValue = default(T);
            bool isUpdated = false;
            bool isCompleted = false;
            SingleAssignmentDisposable sourceSubscription;

            public SampleFrame(SampleFrameObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.sourceSubscription = new SingleAssignmentDisposable();
                this.sourceSubscription.Disposable = this.parent.source.Subscribe(this);
                
                var scheduling = Observable.IntervalFrame(this.parent.frameCount, this.parent.frameCountType)
                    .Subscribe(new SampleFrameTick(this));

                return StableCompositeDisposable.Create(this.sourceSubscription, scheduling);
            }

            void OnNextTick(long _)
            {
                lock (this.gate)
                {
                    if (this.isUpdated)
                    {
                        var value = this.latestValue;
                        this.isUpdated = false;
                        this.observer.OnNext(value);
                    }
                    if (this.isCompleted)
                    {
                        try {
                            this.observer.OnCompleted(); } finally {
                            this.Dispose(); }
                    }
                }
            }

            public override void OnNext(T value)
            {
                lock (this.gate)
                {
                    this.latestValue = value;
                    this.isUpdated = true;
                }
            }

            public override void OnError(Exception error)
            {
                lock (this.gate)
                {
                    try { this.observer.OnError(error); } finally {
                        this.Dispose(); }
                }
            }

            public override void OnCompleted()
            {
                lock (this.gate)
                {
                    this.isCompleted = true;
                    this.sourceSubscription.Dispose();
                }
            }
            class SampleFrameTick : IObserver<long>
            {
                readonly SampleFrame parent;

                public SampleFrameTick(SampleFrame parent)
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
                        if (this.parent.isUpdated)
                        {
                            var value = this.parent.latestValue;
                            this.parent.isUpdated = false;
                            this.parent.observer.OnNext(value);
                        }
                        if (this.parent.isCompleted)
                        {
                            try {
                                this.parent.observer.OnCompleted(); } finally {
                                this.parent.Dispose(); }
                        }
                    }
                }
            }
        }
    }
}