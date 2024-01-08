using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.UnityEngineBridge.Operators
{
    internal class FrameIntervalObservable<T> : OperatorObservableBase<FrameInterval<T>>
    {
        readonly IObservable<T> source;

        public FrameIntervalObservable(IObservable<T> source)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
        }

        protected override IDisposable SubscribeCore(IObserver<FrameInterval<T>> observer, IDisposable cancel)
        {
            return this.source.Subscribe(new FrameInterval(observer, cancel));
        }

        class FrameInterval : OperatorObserverBase<T, FrameInterval<T>>
        {
            int lastFrame;

            public FrameInterval(IObserver<FrameInterval<T>> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                this.lastFrame = UnityEngine.Time.frameCount;
            }

            public override void OnNext(T value)
            {
                var now = UnityEngine.Time.frameCount;
                var span = now - this.lastFrame;
                this.lastFrame = now;

                this.observer.OnNext(new FrameInterval<T>(value, span));
            }

            public override void OnError(Exception error)
            {
                try {
                    this.observer.OnError(error); }
                finally {
                    this.Dispose(); }
            }

            public override void OnCompleted()
            {
                try {
                    this.observer.OnCompleted(); }
                finally {
                    this.Dispose(); }
            }
        }
    }
}