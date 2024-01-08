using System;
using System.Collections.Generic;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Schedulers;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class ObserveOnObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly IScheduler scheduler;

        public ObserveOnObservable(IObservable<T> source, IScheduler scheduler)
            : base(source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.scheduler = scheduler;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            var queueing = this.scheduler as ISchedulerQueueing;
            if (queueing == null)
            {
                return new ObserveOn(this, observer, cancel).Run();
            }
            else
            {
                return new ObserveOn_(this, queueing, observer, cancel).Run();
            }
        }

        class ObserveOn : OperatorObserverBase<T, T>
        {
            class SchedulableAction : IDisposable
            {
                public Notification<T> data;
                public LinkedListNode<SchedulableAction> node;
                public IDisposable schedule;

                public void Dispose()
                {
                    if (this.schedule != null) this.schedule.Dispose();
                    this.schedule = null;

                    if (this.node.List != null)
                    {
                        this.node.List.Remove(this.node);
                    }
                }

                public bool IsScheduled { get { return this.schedule != null; } }
            }

            readonly ObserveOnObservable<T> parent;
            readonly LinkedList<SchedulableAction> actions = new LinkedList<SchedulableAction>();
            bool isDisposed;

            public ObserveOn(ObserveOnObservable<T> parent, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
            }

            public IDisposable Run()
            {
                this.isDisposed = false;

                var sourceDisposable = this.parent.source.Subscribe(this);

                return StableCompositeDisposable.Create(sourceDisposable, Disposable.Create(() =>
                {
                    lock (this.actions)
                    {
                        this.isDisposed = true;

                        while (this.actions.Count > 0)
						{
							// Dispose will both cancel the action (if not already running)
							// and remove it from 'actions'
                            this.actions.First.Value.Dispose();
						}
                    }
                }));
            }

            public override void OnNext(T value)
            {
                this.QueueAction(new Notification<T>.OnNextNotification(value));
            }

            public override void OnError(Exception error)
            {
                this.QueueAction(new Notification<T>.OnErrorNotification(error));
            }

            public override void OnCompleted()
            {
                this.QueueAction(new Notification<T>.OnCompletedNotification());
            }

            private void QueueAction(Notification<T> data)
            {
                var action = new SchedulableAction { data = data };
                lock (this.actions)
                {
                    if (this.isDisposed) return;

                    action.node = this.actions.AddLast(action);
                    this.ProcessNext();
                }
            }

            private void ProcessNext()
            {
                lock (this.actions)
                {
                    if (this.actions.Count == 0 || this.isDisposed)
                        return;

                    var action = this.actions.First.Value;

                    if (action.IsScheduled)
                        return;

                    action.schedule = this.parent.scheduler.Schedule(() =>
                    {
                        try
                        {
                            switch (action.data.Kind)
                            {
                                case NotificationKind.OnNext:
                                    this.observer.OnNext(action.data.Value);
                                    break;
                                case NotificationKind.OnError:
                                    this.observer.OnError(action.data.Exception);
                                    break;
                                case NotificationKind.OnCompleted:
                                    this.observer.OnCompleted();
                                    break;
                            }
                        }
                        finally
                        {
                            lock (this.actions)
                            {
                                action.Dispose();
                            }

                            if (action.data.Kind == NotificationKind.OnNext)
                                this.ProcessNext();
                            else
                                this.Dispose();
                        }
                    });
                }
            }
        }

        class ObserveOn_ : OperatorObserverBase<T, T>
        {
            readonly ObserveOnObservable<T> parent;
            readonly ISchedulerQueueing scheduler;
            readonly BooleanDisposable isDisposed;
            readonly Action<T> onNext;

            public ObserveOn_(ObserveOnObservable<T> parent, ISchedulerQueueing scheduler, IObserver<T> observer, IDisposable cancel) : base(observer, cancel)
            {
                this.parent = parent;
                this.scheduler = scheduler;
                this.isDisposed = new BooleanDisposable();
                this.onNext = new Action<T>(this.OnNext_); // cache delegate
            }

            public IDisposable Run()
            {
                var sourceDisposable = this.parent.source.Subscribe(this);
                return StableCompositeDisposable.Create(sourceDisposable, this.isDisposed);
            }

            void OnNext_(T value)
            {
                this.observer.OnNext(value);
            }

            void OnError_(Exception error)
            {
                try {
                    this.observer.OnError(error); } finally {
                    this.Dispose(); };
            }

            void OnCompleted_(Unit _)
            {
                try {
                    this.observer.OnCompleted(); } finally {
                    this.Dispose(); };
            }

            public override void OnNext(T value)
            {
                this.scheduler.ScheduleQueueing(this.isDisposed, value, this.onNext);
            }

            public override void OnError(Exception error)
            {
                this.scheduler.ScheduleQueueing(this.isDisposed, error, this.OnError_);
            }

            public override void OnCompleted()
            {
                this.scheduler.ScheduleQueueing(this.isDisposed, Unit.Default, this.OnCompleted_);
            }
        }
    }
}