using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.UnityEngineBridge.Operators
{
    internal class FrameTimeIntervalObservable<T> : OperatorObservableBase<TimeInterval<T>>
    {
        readonly IObservable<T> source;
        readonly bool ignoreTimeScale;

        public FrameTimeIntervalObservable(IObservable<T> source, bool ignoreTimeScale)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.ignoreTimeScale = ignoreTimeScale;
        }

        protected override IDisposable SubscribeCore(IObserver<TimeInterval<T>> observer, IDisposable cancel)
        {
            return this.source.Subscribe(new FrameTimeInterval(this, observer, cancel));
        }

        class FrameTimeInterval : OperatorObserverBase<T, TimeInterval<T>>
        {
            readonly FrameTimeIntervalObservable<T> parent;
            float lastTime;

            public FrameTimeInterval(FrameTimeIntervalObservable<T> parent, IObserver<TimeInterval<T>> observer, IDisposable cancel)
                : base(observer, cancel)
            {
                this.parent = parent;
                this.lastTime = (parent.ignoreTimeScale)
                    ? UnityEngine.Time.unscaledTime
                    : UnityEngine.Time.time;
            }

            public override void OnNext(T value)
            {
                var now = (this.parent.ignoreTimeScale)
                    ? UnityEngine.Time.unscaledTime
                    : UnityEngine.Time.time;
                var span = now - this.lastTime;
                this.lastTime = now;

                this.observer.OnNext(new TimeInterval<T>(value, TimeSpan.FromSeconds(span)));
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