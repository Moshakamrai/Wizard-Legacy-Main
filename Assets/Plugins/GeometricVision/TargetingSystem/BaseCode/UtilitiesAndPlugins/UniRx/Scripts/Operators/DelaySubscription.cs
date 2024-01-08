using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Schedulers;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    internal class DelaySubscriptionObservable<T> : OperatorObservableBase<T>
    {
        readonly IObservable<T> source;
        readonly IScheduler scheduler;
        readonly TimeSpan? dueTimeT;
        readonly DateTimeOffset? dueTimeD;

        public DelaySubscriptionObservable(IObservable<T> source,TimeSpan dueTime, IScheduler scheduler)
            : base(scheduler == Scheduler.CurrentThread || source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.scheduler = scheduler;
            this.dueTimeT = dueTime;
        }

        public DelaySubscriptionObservable(IObservable<T> source, DateTimeOffset dueTime, IScheduler scheduler)
            : base(scheduler == Scheduler.CurrentThread || source.IsRequiredSubscribeOnCurrentThread())
        {
            this.source = source;
            this.scheduler = scheduler;
            this.dueTimeD = dueTime;
        }

        protected override IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel)
        {
            if (this.dueTimeT != null)
            {
                var d = new MultipleAssignmentDisposable();
                var dt = Scheduler.Normalize(this.dueTimeT.Value);

                d.Disposable = this.scheduler.Schedule(dt, () =>
                {
                    d.Disposable = this.source.Subscribe(observer);
                });

                return d;
            }
            else
            {
                var d = new MultipleAssignmentDisposable();

                d.Disposable = this.scheduler.Schedule(this.dueTimeD.Value, () =>
                {
                    d.Disposable = this.source.Subscribe(observer);
                });

                return d;
            }
        }
    }
}