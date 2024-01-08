using System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables
{
    public sealed class BooleanDisposable : IDisposable, ICancelable
    {
        public bool IsDisposed { get; private set; }

        public BooleanDisposable()
        {

        }

        internal BooleanDisposable(bool isDisposed)
        {
            this.IsDisposed = isDisposed;
        }

        public void Dispose()
        {
            if (!this.IsDisposed) this.IsDisposed = true;
        }
    }
}