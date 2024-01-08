// for uGUI(from 4.6)
#if !(UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5)

using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Subjects;
using UnityEngine;
using UnityEngine.EventSystems; // require keep for Windows Universal App

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.UnityEngineBridge.Triggers
{
    [DisallowMultipleComponent]
    public class ObservableSelectTrigger : ObservableTriggerBase, IEventSystemHandler, ISelectHandler
    {
        Subject<BaseEventData> onSelect;

        void ISelectHandler.OnSelect(BaseEventData eventData)
        {
            if (this.onSelect != null) this.onSelect.OnNext(eventData);
        }

        public IObservable<BaseEventData> OnSelectAsObservable()
        {
            return this.onSelect ?? (this.onSelect = new Subject<BaseEventData>());
        }

        protected override void RaiseOnCompletedOnDestroy()
        {
            if (this.onSelect != null)
            {
                this.onSelect.OnCompleted();
            }
        }
    }
}


#endif