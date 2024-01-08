// after uGUI(from 4.6)
#if !(UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5)

using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Subjects;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;
using UnityEngine;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.UnityEngineBridge.Triggers
{
    [DisallowMultipleComponent]
    public class ObservableRectTransformTrigger : ObservableTriggerBase
    {
        Subject<Unit> onRectTransformDimensionsChange;

        // Callback that is sent if an associated RectTransform has it's dimensions changed
        void OnRectTransformDimensionsChange()
        {
            if (this.onRectTransformDimensionsChange != null) this.onRectTransformDimensionsChange.OnNext(Unit.Default);
        }

        /// <summary>Callback that is sent if an associated RectTransform has it's dimensions changed.</summary>
        public IObservable<Unit> OnRectTransformDimensionsChangeAsObservable()
        {
            return this.onRectTransformDimensionsChange ?? (this.onRectTransformDimensionsChange = new Subject<Unit>());
        }

        Subject<Unit> onRectTransformRemoved;

        // Callback that is sent if an associated RectTransform is removed
        void OnRectTransformRemoved()
        {
            if (this.onRectTransformRemoved != null) this.onRectTransformRemoved.OnNext(Unit.Default);
        }

        /// <summary>Callback that is sent if an associated RectTransform is removed.</summary>
        public IObservable<Unit> OnRectTransformRemovedAsObservable()
        {
            return this.onRectTransformRemoved ?? (this.onRectTransformRemoved = new Subject<Unit>());
        }

        protected override void RaiseOnCompletedOnDestroy()
        {
            if (this.onRectTransformDimensionsChange != null)
            {
                this.onRectTransformDimensionsChange.OnCompleted();
            }
            if (this.onRectTransformRemoved != null)
            {
                this.onRectTransformRemoved.OnCompleted();
            }
        }

    }
}

#endif