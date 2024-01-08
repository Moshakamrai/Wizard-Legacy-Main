using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Subjects;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Notifiers
{
    /// <summary>Event kind of CountNotifier.</summary>
    public enum CountChangedStatus
    {
        /// <summary>Count incremented.</summary>
        Increment,
        /// <summary>Count decremented.</summary>
        Decrement,
        /// <summary>Count is zero.</summary>
        Empty,
        /// <summary>Count arrived max.</summary>
        Max
    }

    /// <summary>
    /// Notify event of count flag.
    /// </summary>
    public class CountNotifier : IObservable<CountChangedStatus>
    {
        readonly object lockObject = new object();
        readonly Subject<CountChangedStatus> statusChanged = new Subject<CountChangedStatus>();
        readonly int max;

        public int Max { get { return this.max; } }
        public int Count { get; private set; }

        /// <summary>
        /// Setup max count of signal.
        /// </summary>
        public CountNotifier(int max = int.MaxValue)
        {
            if (max <= 0)
            {
                throw new ArgumentException("max");
            }

            this.max = max;
        }

        /// <summary>
        /// Increment count and notify status.
        /// </summary>
        public IDisposable Increment(int incrementCount = 1)
        {
            if (incrementCount < 0)
            {
                throw new ArgumentException("incrementCount");
            }

            lock (this.lockObject)
            {
                if (this.Count == this.Max) return Disposable.Empty;
                else if (incrementCount + this.Count > this.Max)
                    this.Count = this.Max;
                else
                    this.Count += incrementCount;

                this.statusChanged.OnNext(CountChangedStatus.Increment);
                if (this.Count == this.Max) this.statusChanged.OnNext(CountChangedStatus.Max);

                return Disposable.Create(() => this.Decrement(incrementCount));
            }
        }

        /// <summary>
        /// Decrement count and notify status.
        /// </summary>
        public void Decrement(int decrementCount = 1)
        {
            if (decrementCount < 0)
            {
                throw new ArgumentException("decrementCount");
            }

            lock (this.lockObject)
            {
                if (this.Count == 0) return;
                else if (this.Count - decrementCount < 0)
                    this.Count = 0;
                else
                    this.Count -= decrementCount;

                this.statusChanged.OnNext(CountChangedStatus.Decrement);
                if (this.Count == 0) this.statusChanged.OnNext(CountChangedStatus.Empty);
            }
        }

        /// <summary>
        /// Subscribe observer.
        /// </summary>
        public IDisposable Subscribe(IObserver<CountChangedStatus> observer)
        {
            return this.statusChanged.Subscribe(observer);
        }
    }
}