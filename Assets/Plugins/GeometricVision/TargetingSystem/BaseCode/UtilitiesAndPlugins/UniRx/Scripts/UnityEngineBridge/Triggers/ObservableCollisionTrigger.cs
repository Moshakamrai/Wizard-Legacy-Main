using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Subjects;
using UnityEngine; // require keep for Windows Universal App

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.UnityEngineBridge.Triggers
{
    [DisallowMultipleComponent]
    public class ObservableCollisionTrigger : ObservableTriggerBase
    {
        Subject<Collision> onCollisionEnter;

        /// <summary>OnCollisionEnter is called when this collider/rigidbody has begun touching another rigidbody/collider.</summary>
         void OnCollisionEnter(Collision collision)
        {
            if (this.onCollisionEnter != null) this.onCollisionEnter.OnNext(collision);
        }

        /// <summary>OnCollisionEnter is called when this collider/rigidbody has begun touching another rigidbody/collider.</summary>
        public IObservable<Collision> OnCollisionEnterAsObservable()
        {
            return this.onCollisionEnter ?? (this.onCollisionEnter = new Subject<Collision>());
        }

        Subject<Collision> onCollisionExit;

        /// <summary>OnCollisionExit is called when this collider/rigidbody has stopped touching another rigidbody/collider.</summary>
         void OnCollisionExit(Collision collisionInfo)
        {
            if (this.onCollisionExit != null) this.onCollisionExit.OnNext(collisionInfo);
        }

        /// <summary>OnCollisionExit is called when this collider/rigidbody has stopped touching another rigidbody/collider.</summary>
        public IObservable<Collision> OnCollisionExitAsObservable()
        {
            return this.onCollisionExit ?? (this.onCollisionExit = new Subject<Collision>());
        }

        Subject<Collision> onCollisionStay;

        /// <summary>OnCollisionStay is called once per frame for every collider/rigidbody that is touching rigidbody/collider.</summary>
         void OnCollisionStay(Collision collisionInfo)
        {
            if (this.onCollisionStay != null) this.onCollisionStay.OnNext(collisionInfo);
        }

        /// <summary>OnCollisionStay is called once per frame for every collider/rigidbody that is touching rigidbody/collider.</summary>
        public IObservable<Collision> OnCollisionStayAsObservable()
        {
            return this.onCollisionStay ?? (this.onCollisionStay = new Subject<Collision>());
        }

        protected override void RaiseOnCompletedOnDestroy()
        {
            if (this.onCollisionEnter != null)
            {
                this.onCollisionEnter.OnCompleted();
            }
            if (this.onCollisionExit != null)
            {
                this.onCollisionExit.OnCompleted();
            }
            if (this.onCollisionStay != null)
            {
                this.onCollisionStay.OnCompleted();
            }
        }
    }
}