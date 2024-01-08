using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Subjects;
using UnityEngine; // require keep for Windows Universal App

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.UnityEngineBridge.Triggers
{
    [DisallowMultipleComponent]
    public class ObservableTrigger2DTrigger : ObservableTriggerBase
    {
        Subject<Collider2D> onTriggerEnter2D;

        /// <summary>Sent when another object enters a trigger collider attached to this object (2D physics only).</summary>
        void OnTriggerEnter2D(Collider2D other)
        {
            if (this.onTriggerEnter2D != null) this.onTriggerEnter2D.OnNext(other);
        }

        /// <summary>Sent when another object enters a trigger collider attached to this object (2D physics only).</summary>
        public IObservable<Collider2D> OnTriggerEnter2DAsObservable()
        {
            return this.onTriggerEnter2D ?? (this.onTriggerEnter2D = new Subject<Collider2D>());
        }

        Subject<Collider2D> onTriggerExit2D;

        /// <summary>Sent when another object leaves a trigger collider attached to this object (2D physics only).</summary>
        void OnTriggerExit2D(Collider2D other)
        {
            if (this.onTriggerExit2D != null) this.onTriggerExit2D.OnNext(other);
        }

        /// <summary>Sent when another object leaves a trigger collider attached to this object (2D physics only).</summary>
        public IObservable<Collider2D> OnTriggerExit2DAsObservable()
        {
            return this.onTriggerExit2D ?? (this.onTriggerExit2D = new Subject<Collider2D>());
        }

        Subject<Collider2D> onTriggerStay2D;

        /// <summary>Sent each frame where another object is within a trigger collider attached to this object (2D physics only).</summary>
        void OnTriggerStay2D(Collider2D other)
        {
            if (this.onTriggerStay2D != null) this.onTriggerStay2D.OnNext(other);
        }

        /// <summary>Sent each frame where another object is within a trigger collider attached to this object (2D physics only).</summary>
        public IObservable<Collider2D> OnTriggerStay2DAsObservable()
        {
            return this.onTriggerStay2D ?? (this.onTriggerStay2D = new Subject<Collider2D>());
        }

        protected override void RaiseOnCompletedOnDestroy()
        {
            if (this.onTriggerEnter2D != null)
            {
                this.onTriggerEnter2D.OnCompleted();
            }
            if (this.onTriggerExit2D != null)
            {
                this.onTriggerExit2D.OnCompleted();
            }
            if (this.onTriggerStay2D != null)
            {
                this.onTriggerStay2D.OnCompleted();
            }
        }
    }
}