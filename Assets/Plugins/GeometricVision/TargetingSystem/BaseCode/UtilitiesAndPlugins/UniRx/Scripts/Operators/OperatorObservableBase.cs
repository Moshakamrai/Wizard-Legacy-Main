using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Schedulers;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Operators
{
    // implements note : all field must be readonly.
    public abstract class OperatorObservableBase<T> : IObservable<T>, IOptimizedObservable<T>
    {
        readonly bool isRequiredSubscribeOnCurrentThread;

        public OperatorObservableBase(bool isRequiredSubscribeOnCurrentThread)
        {
            this.isRequiredSubscribeOnCurrentThread = isRequiredSubscribeOnCurrentThread;
        }

        public bool IsRequiredSubscribeOnCurrentThread()
        {
            return this.isRequiredSubscribeOnCurrentThread;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            var subscription = new SingleAssignmentDisposable();

            // note:
            // does not make the safe observer, it breaks exception durability.
            // var safeObserver = Observer.CreateAutoDetachObserver<T>(observer, subscription);

            if (this.isRequiredSubscribeOnCurrentThread && Scheduler.IsCurrentThreadSchedulerScheduleRequired)
            {
                Scheduler.CurrentThread.Schedule(() => subscription.Disposable = this.SubscribeCore(observer, subscription));
            }
            else
            {
                subscription.Disposable = this.SubscribeCore(observer, subscription);
            }

            return subscription;
        }

        protected abstract IDisposable SubscribeCore(IObserver<T> observer, IDisposable cancel);
    }
}