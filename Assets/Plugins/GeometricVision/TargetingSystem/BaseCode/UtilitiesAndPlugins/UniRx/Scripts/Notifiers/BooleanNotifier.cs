using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Subjects;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Notifiers
{
    /// <summary>
    /// Notify boolean flag.
    /// </summary>
    public class BooleanNotifier : IObservable<bool>
    {
        readonly Subject<bool> boolTrigger = new Subject<bool>();

        bool boolValue;
        /// <summary>Current flag value</summary>
        public bool Value
        {
            get { return this.boolValue; }
            set
            {
                this.boolValue = value;
                this.boolTrigger.OnNext(value);
            }
        }

        /// <summary>
        /// Setup initial flag.
        /// </summary>
        public BooleanNotifier(bool initialValue = false)
        {
            this.Value = initialValue;
        }

        /// <summary>
        /// Set and raise true if current value isn't true.
        /// </summary>
        public void TurnOn()
        {
            if (this.Value != true)
            {
                this.Value = true;
            }
        }

        /// <summary>
        /// Set and raise false if current value isn't false.
        /// </summary>
        public void TurnOff()
        {
            if (this.Value != false)
            {
                this.Value = false;
            }
        }

        /// <summary>
        /// Set and raise reverse value.
        /// </summary>
        public void SwitchValue()
        {
            this.Value = !this.Value;
        }


        /// <summary>
        /// Subscribe observer.
        /// </summary>
        public IDisposable Subscribe(IObserver<bool> observer)
        {
            return this.boolTrigger.Subscribe(observer);
        }
    }
}