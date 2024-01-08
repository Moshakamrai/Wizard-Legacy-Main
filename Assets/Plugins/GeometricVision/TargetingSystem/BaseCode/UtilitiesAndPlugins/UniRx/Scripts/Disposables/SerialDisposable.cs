using System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables
{
    public sealed class SerialDisposable : IDisposable, ICancelable
    {
        readonly object gate = new object();
        IDisposable current;
        bool disposed;

        public bool IsDisposed { get { lock (this.gate) { return this.disposed; } } }

        public IDisposable Disposable
        {
            get
            {
                return this.current;
            }
            set
            {
                var shouldDispose = false;
                var old = default(IDisposable);
                lock (this.gate)
                {
                    shouldDispose = this.disposed;
                    if (!shouldDispose)
                    {
                        old = this.current;
                        this.current = value;
                    }
                }
                if (old != null)
                {
                    old.Dispose();
                }
                if (shouldDispose && value != null)
                {
                    value.Dispose();
                }
            }
        }

        public void Dispose()
        {
            var old = default(IDisposable);

            lock (this.gate)
            {
                if (!this.disposed)
                {
                    this.disposed = true;
                    old = this.current;
                    this.current = null;
                }
            }

            if (old != null)
            {
                old.Dispose();
            }
        }
    }
}