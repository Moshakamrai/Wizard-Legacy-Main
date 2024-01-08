// after uGUI(from 4.6)
#if !(UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5)

using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Subjects;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;
using UnityEngine;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.UnityEngineBridge.Triggers
{
    [DisallowMultipleComponent]
    public class ObservableTransformChangedTrigger : ObservableTriggerBase
    {
        Subject<Unit> onBeforeTransformParentChanged;

        // Callback sent to the graphic before a Transform parent change occurs
        void OnBeforeTransformParentChanged()
        {
            if (this.onBeforeTransformParentChanged != null) this.onBeforeTransformParentChanged.OnNext(Unit.Default);
        }

        /// <summary>Callback sent to the graphic before a Transform parent change occurs.</summary>
        public IObservable<Unit> OnBeforeTransformParentChangedAsObservable()
        {
            return this.onBeforeTransformParentChanged ?? (this.onBeforeTransformParentChanged = new Subject<Unit>());
        }

        Subject<Unit> onTransformParentChanged;

        // This function is called when the parent property of the transform of the GameObject has changed
        void OnTransformParentChanged()
        {
            if (this.onTransformParentChanged != null) this.onTransformParentChanged.OnNext(Unit.Default);
        }

        /// <summary>This function is called when the parent property of the transform of the GameObject has changed.</summary>
        public IObservable<Unit> OnTransformParentChangedAsObservable()
        {
            return this.onTransformParentChanged ?? (this.onTransformParentChanged = new Subject<Unit>());
        }

        Subject<Unit> onTransformChildrenChanged;

        // This function is called when the list of children of the transform of the GameObject has changed
        void OnTransformChildrenChanged()
        {
            if (this.onTransformChildrenChanged != null) this.onTransformChildrenChanged.OnNext(Unit.Default);
        }

        /// <summary>This function is called when the list of children of the transform of the GameObject has changed.</summary>
        public IObservable<Unit> OnTransformChildrenChangedAsObservable()
        {
            return this.onTransformChildrenChanged ?? (this.onTransformChildrenChanged = new Subject<Unit>());
        }

        protected override void RaiseOnCompletedOnDestroy()
        {
            if (this.onBeforeTransformParentChanged != null)
            {
                this.onBeforeTransformParentChanged.OnCompleted();
            }
            if (this.onTransformParentChanged != null)
            {
                this.onTransformParentChanged.OnCompleted();
            }
            if (this.onTransformChildrenChanged != null)
            {
                this.onTransformChildrenChanged.OnCompleted();
            }
        }
    }
}

#endif