// after uGUI(from 4.6)
#if !(UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5)

using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Subjects;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;
using UnityEngine;

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.UnityEngineBridge.Triggers
{
    [DisallowMultipleComponent]
    public class ObservableCanvasGroupChangedTrigger : ObservableTriggerBase
    {
        Subject<Unit> onCanvasGroupChanged;

        // Callback that is sent if the canvas group is changed
        void OnCanvasGroupChanged()
        {
            if (this.onCanvasGroupChanged != null) this.onCanvasGroupChanged.OnNext(Unit.Default);
        }

        /// <summary>Callback that is sent if the canvas group is changed.</summary>
        public IObservable<Unit> OnCanvasGroupChangedAsObservable()
        {
            return this.onCanvasGroupChanged ?? (this.onCanvasGroupChanged = new Subject<Unit>());
        }

        protected override void RaiseOnCompletedOnDestroy()
        {
            if (this.onCanvasGroupChanged != null)
            {
                this.onCanvasGroupChanged.OnCompleted();
            }
        }
    }
}

#endif