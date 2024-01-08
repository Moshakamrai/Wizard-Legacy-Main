using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Subjects;
using UnityEngine; // require keep for Windows Universal App

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.UnityEngineBridge.Triggers
{
    [DisallowMultipleComponent]
    public class ObservableCollision2DTrigger : ObservableTriggerBase
    {
        Subject<Collision2D> onCollisionEnter2D;

        /// <summary>Sent when an incoming collider makes contact with this object's collider (2D physics only).</summary>
         void OnCollisionEnter2D(Collision2D coll)
        {
            if (this.onCollisionEnter2D != null) this.onCollisionEnter2D.OnNext(coll);
        }

        /// <summary>Sent when an incoming collider makes contact with this object's collider (2D physics only).</summary>
        public IObservable<Collision2D> OnCollisionEnter2DAsObservable()
        {
            return this.onCollisionEnter2D ?? (this.onCollisionEnter2D = new Subject<Collision2D>());
        }

        Subject<Collision2D> onCollisionExit2D;

        /// <summary>Sent when a collider on another object stops touching this object's collider (2D physics only).</summary>
         void OnCollisionExit2D(Collision2D coll)
        {
            if (this.onCollisionExit2D != null) this.onCollisionExit2D.OnNext(coll);
        }

        /// <summary>Sent when a collider on another object stops touching this object's collider (2D physics only).</summary>
        public IObservable<Collision2D> OnCollisionExit2DAsObservable()
        {
            return this.onCollisionExit2D ?? (this.onCollisionExit2D = new Subject<Collision2D>());
        }

        Subject<Collision2D> onCollisionStay2D;

        /// <summary>Sent each frame where a collider on another object is touching this object's collider (2D physics only).</summary>
         void OnCollisionStay2D(Collision2D coll)
        {
            if (this.onCollisionStay2D != null) this.onCollisionStay2D.OnNext(coll);
        }

        /// <summary>Sent each frame where a collider on another object is touching this object's collider (2D physics only).</summary>
        public IObservable<Collision2D> OnCollisionStay2DAsObservable()
        {
            return this.onCollisionStay2D ?? (this.onCollisionStay2D = new Subject<Collision2D>());
        }

        protected override void RaiseOnCompletedOnDestroy()
        {
            if (this.onCollisionEnter2D != null)
            {
                this.onCollisionEnter2D.OnCompleted();
            }
            if (this.onCollisionExit2D != null)
            {
                this.onCollisionExit2D.OnCompleted();
            }
            if (this.onCollisionStay2D != null)
            {
                this.onCollisionStay2D.OnCompleted();
            }
        }
    }
}