using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Schedulers;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System
{
    public interface IOptimizedObservable<T> : IObservable<T>
    {
        bool IsRequiredSubscribeOnCurrentThread();
    }

    public static class OptimizedObservableExtensions
    {
        public static bool IsRequiredSubscribeOnCurrentThread<T>(this IObservable<T> source)
        {
            var obs = source as IOptimizedObservable<T>;
            if (obs == null) return true;

            return obs.IsRequiredSubscribeOnCurrentThread();
        }

        public static bool IsRequiredSubscribeOnCurrentThread<T>(this IObservable<T> source, IScheduler scheduler)
        {
            if (scheduler == Scheduler.CurrentThread) return true;

            return IsRequiredSubscribeOnCurrentThread(source);
        }
    }
}