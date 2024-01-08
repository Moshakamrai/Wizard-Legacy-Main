using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Schedulers;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Subjects;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Notifiers
{
    /// <summary>
    /// Notify value on setuped scheduler.
    /// </summary>
    public class ScheduledNotifier<T> : IObservable<T>, IProgress<T>
    {
        readonly IScheduler scheduler;
        readonly Subject<T> trigger = new Subject<T>();

        /// <summary>
        /// Use scheduler is Scheduler.DefaultSchedulers.ConstantTimeOperations.
        /// </summary>
        public ScheduledNotifier()
        {
            this.scheduler = Scheduler.DefaultSchedulers.ConstantTimeOperations;
        }
        /// <summary>
        /// Use scheduler is argument's scheduler.
        /// </summary>
        public ScheduledNotifier(IScheduler scheduler)
        {
            if (scheduler == null)
            {
                throw new ArgumentNullException("scheduler");
            }

            this.scheduler = scheduler;
        }

        /// <summary>
        /// Push value to subscribers on setuped scheduler.
        /// </summary>
        public void Report(T value)
        {
            this.scheduler.Schedule(() => this.trigger.OnNext(value));
        }

        /// <summary>
        /// Push value to subscribers on setuped scheduler.
        /// </summary>
        public IDisposable Report(T value, TimeSpan dueTime)
        {
            var cancel = this.scheduler.Schedule(dueTime, () => this.trigger.OnNext(value));
            return cancel;
        }

        /// <summary>
        /// Push value to subscribers on setuped scheduler.
        /// </summary>
        public IDisposable Report(T value, DateTimeOffset dueTime)
        {
            var cancel = this.scheduler.Schedule(dueTime, () => this.trigger.OnNext(value));
            return cancel;
        }

        /// <summary>
        /// Subscribe observer.
        /// </summary>
        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (observer == null)
            {
                throw new ArgumentNullException("observer");
            }

            return this.trigger.Subscribe(observer);
        }
    }
}