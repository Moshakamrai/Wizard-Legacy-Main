// for uGUI(from 4.6)
#if !(UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5)

using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Subjects;
using UnityEngine;
using UnityEngine.EventSystems; // require keep for Windows Universal App

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.UnityEngineBridge.Triggers
{
    [DisallowMultipleComponent]
    public class ObservableDeselectTrigger : ObservableTriggerBase, IEventSystemHandler, IDeselectHandler
    {
        Subject<BaseEventData> onDeselect;

        void IDeselectHandler.OnDeselect(BaseEventData eventData)
        {
            if (this.onDeselect != null) this.onDeselect.OnNext(eventData);
        }

        public IObservable<BaseEventData> OnDeselectAsObservable()
        {
            return this.onDeselect ?? (this.onDeselect = new Subject<BaseEventData>());
        }

        protected override void RaiseOnCompletedOnDestroy()
        {
            if (this.onDeselect != null)
            {
                this.onDeselect.OnCompleted();
            }
        }
    }
}


#endif