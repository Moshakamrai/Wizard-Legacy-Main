using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Subjects;
using UnityEngine; // require keep for Windows Universal App

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.UnityEngineBridge.Triggers
{
    [DisallowMultipleComponent]
    public class ObservableTriggerTrigger : ObservableTriggerBase
    {
        Subject<Collider> onTriggerEnter;

        /// <summary>OnTriggerEnter is called when the Collider other enters the trigger.</summary>
        void OnTriggerEnter(Collider other)
        {
            if (this.onTriggerEnter != null) this.onTriggerEnter.OnNext(other);
        }

        /// <summary>OnTriggerEnter is called when the Collider other enters the trigger.</summary>
        public IObservable<Collider> OnTriggerEnterAsObservable()
        {
            return this.onTriggerEnter ?? (this.onTriggerEnter = new Subject<Collider>());
        }

        Subject<Collider> onTriggerExit;

        /// <summary>OnTriggerExit is called when the Collider other has stopped touching the trigger.</summary>
        void OnTriggerExit(Collider other)
        {
            if (this.onTriggerExit != null) this.onTriggerExit.OnNext(other);
        }

        /// <summary>OnTriggerExit is called when the Collider other has stopped touching the trigger.</summary>
        public IObservable<Collider> OnTriggerExitAsObservable()
        {
            return this.onTriggerExit ?? (this.onTriggerExit = new Subject<Collider>());
        }

        Subject<Collider> onTriggerStay;

        /// <summary>OnTriggerStay is called once per frame for every Collider other that is touching the trigger.</summary>
        void OnTriggerStay(Collider other)
        {
            if (this.onTriggerStay != null) this.onTriggerStay.OnNext(other);
        }

        /// <summary>OnTriggerStay is called once per frame for every Collider other that is touching the trigger.</summary>
        public IObservable<Collider> OnTriggerStayAsObservable()
        {
            return this.onTriggerStay ?? (this.onTriggerStay = new Subject<Collider>());
        }
        
        protected override void RaiseOnCompletedOnDestroy()
        {
            if (this.onTriggerEnter != null)
            {
                this.onTriggerEnter.OnCompleted();
            }
            if (this.onTriggerExit != null)
            {
                this.onTriggerExit.OnCompleted();
            }
            if (this.onTriggerStay != null)
            {
                this.onTriggerStay.OnCompleted();
            }
        }
    }
}