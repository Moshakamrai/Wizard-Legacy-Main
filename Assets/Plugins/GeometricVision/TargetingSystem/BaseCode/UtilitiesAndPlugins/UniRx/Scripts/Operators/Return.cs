﻿using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Schedulers;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class ReturnObservable<T> : OperatorObservableBase<T>
    {
        readonly T value;
        readonly IScheduler scheduler;

        public ReturnObservable(T value, IScheduler scheduler)
            : base(scheduler == Scheduler.CurrentThread)
        {
            this.value = value;
            this.scheduler = scheduler;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            observer = new Return(observer, cancel);

            if (this.scheduler == Scheduler.Immediate)
            {
                observer.OnNext(this.value);
                observer.OnCompleted();
                return Disposable.Empty;
            }
            else
            {
                return this.scheduler.Schedule(() =>
                {
                    observer.OnNext(this.value);
                    observer.OnCompleted();
                });
            }
        }

        class Return : OperatorObserverBase<T, T>
        {
            public Return(IObserver<T> observer, IDisposable cancel)
                : base(observer, cancel)
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

    internal class ImmediateReturnObservable<T> : IObservable<T>, IOptimizedObservable<T>
    {
        readonly T value;

        public ImmediateReturnObservable(T value)
        {
            this.value = value;
        }

        public bool IsRequiredSubscribeOnCurrentThread()
        {
            return false;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            observer.OnNext(this.value);
            observer.OnCompleted();
            return Disposable.Empty;
        }
    }

    internal class ImmutableReturnUnitObservable : IObservable<Unit>, IOptimizedObservable<Unit>
    {
        internal static ImmutableReturnUnitObservable Instance = new ImmutableReturnUnitObservable();

        ImmutableReturnUnitObservable()
        {

        }

        public bool IsRequiredSubscribeOnCurrentThread()
        {
            return false;
        }

        public IDisposable Subscribe(IObserver<Unit> observer)
        {
            observer.OnNext(Unit.Default);
            observer.OnCompleted();
            return Disposable.Empty;
        }
    }

    internal class ImmutableReturnTrueObservable : IObservable<bool>, IOptimizedObservable<bool>
    {
        internal static ImmutableReturnTrueObservable Instance = new ImmutableReturnTrueObservable();

        ImmutableReturnTrueObservable()
        {

        }

        public bool IsRequiredSubscribeOnCurrentThread()
        {
            return false;
        }

        public IDisposable Subscribe(IObserver<bool> observer)
        {
            observer.OnNext(true);
            observer.OnCompleted();
            return Disposable.Empty;
        }
    }

    internal class ImmutableReturnFalseObservable : IObservable<bool>, IOptimizedObservable<bool>
    {
        internal static ImmutableReturnFalseObservable Instance = new ImmutableReturnFalseObservable();

        ImmutableReturnFalseObservable()
        {

        }

        public bool IsRequiredSubscribeOnCurrentThread()
        {
            return false;
        }

        public IDisposable Subscribe(IObserver<bool> observer)
        {
            observer.OnNext(false);
            observer.OnCompleted();
            return Disposable.Empty;
        }
    }

    internal class ImmutableReturnInt32Observable : IObservable<int>, IOptimizedObservable<int>
    {
        static ImmutableReturnInt32Observable[] Caches = new ImmutableReturnInt32Observable[]
        {
            new ImmutableReturnInt32Observable(-1),
            new ImmutableReturnInt32Observable(0),
            new ImmutableReturnInt32Observable(1),
            new ImmutableReturnInt32Observable(2),
            new ImmutableReturnInt32Observable(3),
            new ImmutableReturnInt32Observable(4),
            new ImmutableReturnInt32Observable(5),
            new ImmutableReturnInt32Observable(6),
            new ImmutableReturnInt32Observable(7),
            new ImmutableReturnInt32Observable(8),
            new ImmutableReturnInt32Observable(9),
        };

        public static IObservable<int> GetInt32Observable(int x)
        {
            if (-1 <= x && x <= 9)
            {
                return Caches[x + 1];
            }

            return new ImmediateReturnObservable<int>(x);
        }

        readonly int x;

        ImmutableReturnInt32Observable(int x)
        {
            this.x = x;
        }

        public bool IsRequiredSubscribeOnCurrentThread()
        {
            return false;
        }

        public IDisposable Subscribe(IObserver<int> observer)
        {
            observer.OnNext(this.x);
            observer.OnCompleted();
            return Disposable.Empty;
        }
    }
}