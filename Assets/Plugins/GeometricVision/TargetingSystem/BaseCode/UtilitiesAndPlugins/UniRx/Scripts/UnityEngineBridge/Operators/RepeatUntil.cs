using System;
using System.Collections;
using System.Collections.Generic;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Schedulers;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;
using UnityEngine;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.UnityEngineBridge.Operators
{
    internal class RepeatUntilObservable<T> : OperatorObservableBase<T>
    {
        readonly IEnumerable<IObservable<T>> sources;
        readonly IObservable<Unit> trigger;
        readonly GameObject lifeTimeChecker;

        public RepeatUntilObservable(IEnumerable<IObservable<T>> sources, IObservable<Unit> trigger, GameObject lifeTimeChecker)
            : base(true)
        {
            this.sources = sources;
            this.trigger = trigger;
            this.lifeTimeChecker = lifeTimeChecker;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            return new RepeatUntil(this, observer, cancel).Run();
        }

        class RepeatUntil : OperatorObserverBase<T, T>
        {
            readonly RepeatUntilObservable<T> parent;
            readonly object gate = new object();

            IEnumerator<IObservable<T>> e;
            SerialDisposable subscription;
            SingleAssignmentDisposable schedule;
            Action nextSelf;
            bool isStopped;
            bool isDisposed;
            bool isFirstSubscribe;
            IDisposable stopper;

            public RepeatUntil(RepeatUntilObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.isFirstSubscribe = true;
                this.isDisposed = false;
                this.isStopped = false;
                this.e = this.parent.sources.GetEnumerator();
                this.subscription = new SerialDisposable();
                this.schedule = new SingleAssignmentDisposable();

                this.stopper = this.parent.trigger.Subscribe(_ =>
                {
                    lock (this.gate)
                    {
                        this.isStopped = true;
                        this.e.Dispose();
                        this.subscription.Dispose();
                        this.schedule.Dispose();
                        this.observer.OnCompleted();
                    }
                }, this.observer.OnError);

                this.schedule.Disposable = Schedulers.Scheduler.CurrentThread.Schedule(this.RecursiveRun);

                return new CompositeDisposable(this.schedule, this.subscription, this.stopper, Disposable.Create(() =>
                {
                    lock (this.gate)
                    {
                        this.isDisposed = true;
                        this.e.Dispose();
                    }
                }));
            }

            void RecursiveRun(Action self)
            {
                lock (this.gate)
                {
                    this.nextSelf = self;
                    if (this.isDisposed) return;
                    if (this.isStopped) return;

                    var current = default(IObservable<T>);
                    var hasNext = false;
                    var ex = default(Exception);

                    try
                    {
                        hasNext = this.e.MoveNext();
                        if (hasNext)
                        {
                            current = this.e.Current;
                            if (current == null) throw new InvalidOperationException("sequence is null.");
                        }
                        else
                        {
                            this.e.Dispose();
                        }
                    }
                    catch (Exception exception)
                    {
                        ex = exception;
                        this.e.Dispose();
                    }

                    if (ex != null)
                    {
                        this.stopper.Dispose();
                        this.observer.OnError(ex);
                        return;
                    }

                    if (!hasNext)
                    {
                        this.stopper.Dispose();
                        this.observer.OnCompleted();
                        return;
                    }

                    var source = this.e.Current;
                    var d = new SingleAssignmentDisposable();
                    this.subscription.Disposable = d;

                    if (this.isFirstSubscribe)
                    {
                        this.isFirstSubscribe = false;
                        d.Disposable = source.Subscribe(this);
                    }
                    else
                    {
                        MainThreadDispatcher.SendStartCoroutine(SubscribeAfterEndOfFrame(d, source, this, this.parent.lifeTimeChecker));
                    }
                }
            }

            static IEnumerator SubscribeAfterEndOfFrame(SingleAssignmentDisposable d, IObservable<T> source, IObserver<T> observer, GameObject lifeTimeChecker)
            {
                yield return YieldInstructionCache.WaitForEndOfFrame;
                if (!d.IsDisposed && lifeTimeChecker != null)
                {
                    d.Disposable = source.Subscribe(observer);
                }
            }

            public override void OnNext(T value)
            {
                this.observer.OnNext(value);
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
                if (!this.isDisposed)
                {
                    this.nextSelf();
                }
                else
                {
                    this.e.Dispose();
                    if (!this.isDisposed)
                    {
                        try {
                            this.observer.OnCompleted(); }
                        finally {
                            this.Dispose(); }
                    }
                }
            }
        }
    }
}