using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Subjects;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;
using UnityEngine; // require keep for Windows Universal App

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.UnityEngineBridge.Triggers
{
    [DisallowMultipleComponent]
    public class ObservableEnableTrigger : ObservableTriggerBase
    {
        Subject<Unit> onEnable;

        /// <summary>This function is called when the object becomes enabled and active.</summary>
        void OnEnable()
        {
            if (this.onEnable != null) this.onEnable.OnNext(Unit.Default);
        }

        /// <summary>This function is called when the object becomes enabled and active.</summary>
        public IObservable<Unit> OnEnableAsObservable()
        {
            return this.onEnable ?? (this.onEnable = new Subject<Unit>());
        }

        Subject<Unit> onDisable;

        /// <summary>This function is called when the behaviour becomes disabled () or inactive.</summary>
        void OnDisable()
        {
            if (this.onDisable != null) this.onDisable.OnNext(Unit.Default);
        }

        /// <summary>This function is called when the behaviour becomes disabled () or inactive.</summary>
        public IObservable<Unit> OnDisableAsObservable()
        {
            return this.onDisable ?? (this.onDisable = new Subject<Unit>());
        }

        protected override void RaiseOnCompletedOnDestroy()
        {
            if (this.onEnable != null)
            {
                this.onEnable.OnCompleted();
            }
            if (this.onDisable != null)
            {
                this.onDisable.OnCompleted();
            }
        }
    }
}