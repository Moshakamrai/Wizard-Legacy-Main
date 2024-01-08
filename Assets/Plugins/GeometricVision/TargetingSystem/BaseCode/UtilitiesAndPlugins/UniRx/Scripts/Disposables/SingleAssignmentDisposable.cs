using System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables
{
    // should be use Interlocked.CompareExchange for Threadsafe?
    // but CompareExchange cause ExecutionEngineException on iOS.
    // AOT...
    // use lock instead

    public sealed class SingleAssignmentDisposable : IDisposable, ICancelable
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
                var old = default(IDisposable);
                bool alreadyDisposed;
                lock (this.gate)
                {
                    alreadyDisposed = this.disposed;
                    old = this.current;
                    if (!alreadyDisposed)
                    {
                        if (value == null) return;
                        this.current = value;
                    }
                }

                if (alreadyDisposed && value != null)
                {
                    value.Dispose();
                    return;
                }

                if (old != null) throw new InvalidOperationException("Disposable is already set");
            }
        }


        public void Dispose()
        {
            IDisposable old = null;

            lock (this.gate)
            {
                if (!this.disposed)
                {
                    this.disposed = true;
                    old = this.current;
                    this.current = null;
                }
            }

            if (old != null) old.Dispose();
        }
    }
}