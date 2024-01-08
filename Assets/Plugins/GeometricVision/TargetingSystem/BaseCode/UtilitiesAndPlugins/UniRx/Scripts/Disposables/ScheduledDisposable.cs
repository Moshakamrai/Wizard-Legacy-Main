using System;
using System.Threading;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Schedulers;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables
{
    public sealed class ScheduledDisposable : ICancelable
    {
        private readonly IScheduler scheduler;
        private volatile IDisposable disposable;
        private int isDisposed = 0;

        public ScheduledDisposable(IScheduler scheduler, IDisposable disposable)
        {
            this.scheduler = scheduler;
            this.disposable = disposable;
        }

        public IScheduler Scheduler
        {
            get { return this.scheduler; }
        }

        public IDisposable Disposable
        {
            get { return this.disposable; }
        }

        public bool IsDisposed
        {
            get { return this.isDisposed != 0; }
        }

        public void Dispose()
        {
            this.Scheduler.Schedule(this.DisposeInner);
        }

        private void DisposeInner()
        {
            if (Interlocked.Increment(ref this.isDisposed) == 1)
            {
                this.disposable.Dispose();
            }
        }
    }
}
