using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Subjects;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;
using UnityEngine; // require keep for Windows Universal App

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.UnityEngineBridge.Triggers
{
    [DisallowMultipleComponent]
    public class ObservableVisibleTrigger : ObservableTriggerBase
    {
        Subject<Unit> onBecameInvisible;

        /// <summary>OnBecameInvisible is called when the renderer is no longer visible by any camera.</summary>
        void OnBecameInvisible()
        {
            if (this.onBecameInvisible != null) this.onBecameInvisible.OnNext(Unit.Default);
        }

        /// <summary>OnBecameInvisible is called when the renderer is no longer visible by any camera.</summary>
        public IObservable<Unit> OnBecameInvisibleAsObservable()
        {
            return this.onBecameInvisible ?? (this.onBecameInvisible = new Subject<Unit>());
        }

        Subject<Unit> onBecameVisible;

        /// <summary>OnBecameVisible is called when the renderer became visible by any camera.</summary>
        void OnBecameVisible()
        {
            if (this.onBecameVisible != null) this.onBecameVisible.OnNext(Unit.Default);
        }

        /// <summary>OnBecameVisible is called when the renderer became visible by any camera.</summary>
        public IObservable<Unit> OnBecameVisibleAsObservable()
        {
            return this.onBecameVisible ?? (this.onBecameVisible = new Subject<Unit>());
        }

        protected override void RaiseOnCompletedOnDestroy()
        {
            if (this.onBecameInvisible != null)
            {
                this.onBecameInvisible.OnCompleted();
            }
            if (this.onBecameVisible != null)
            {
                this.onBecameVisible.OnCompleted();
            }
        }
    }
}