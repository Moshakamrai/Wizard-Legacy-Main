using System;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables
{
    public sealed class MultipleAssignmentDisposable : IDisposable, ICancelable
    {
        static readonly BooleanDisposable True = new BooleanDisposable(true);

        object gate = new object();
        IDisposable current;

        public bool IsDisposed
        {
            get
            {
                lock (this.gate)
                {
                    return this.current == True;
                }
            }
        }

        public IDisposable Disposable
        {
            get
            {
                lock (this.gate)
                {
                    return (this.current == True)
                        ? Disposables.Disposable.Empty
                        : this.current;
                }
            }
            set
            {
                var shouldDispose = false;
                lock (this.gate)
                {
                    shouldDispose = (this.current == True);
                    if (!shouldDispose)
                    {
                        this.current = value;
                    }
                }
                if (shouldDispose && value != null)
                {
                    value.Dispose();
                }
            }
        }

        public void Dispose()
        {
            IDisposable old = null;

            lock (this.gate)
            {
                if (this.current != True)
                {
                    old = this.current;
                    this.current = True;
                }
            }

            if (old != null) old.Dispose();
        }
    }
}