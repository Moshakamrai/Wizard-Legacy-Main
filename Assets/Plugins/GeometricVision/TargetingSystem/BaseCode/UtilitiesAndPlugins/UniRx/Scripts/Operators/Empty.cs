using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Schedulers;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class EmptyObservable<T> : OperatorObservableBase<T>
    {
        readonly IScheduler scheduler;

        public EmptyObservable(IScheduler scheduler)
            : base(false)
        {
            this.scheduler = scheduler;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            observer = new Empty(observer, cancel);

            if (this.scheduler == Scheduler.Immediate)
            {
                observer.OnCompleted();
                return Disposable.Empty;
            }
            else
            {
                return this.scheduler.Schedule(observer.OnCompleted);
            }
        }

        class Empty : OperatorObserverBase<T, T>
        {
            public Empty(IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
            }

            public override void OnNext(T value)
            {
                try
                {
                    this.observer.OnNext(value);
                }
                catch
                {
                    this.Dispose();
                    throw;
                }
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

    internal class ImmutableEmptyObservable<T> : IObservable<T>, IOptimizedObservable<T>
    {
        internal static ImmutableEmptyObservable<T> Instance = new ImmutableEmptyObservable<T>();

        ImmutableEmptyObservable()
        {

        }

        public bool IsRequiredSubscribeOnCurrentThread()
        {
            return false;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            observer.OnCompleted();
            return Disposable.Empty;
        }
    }
}