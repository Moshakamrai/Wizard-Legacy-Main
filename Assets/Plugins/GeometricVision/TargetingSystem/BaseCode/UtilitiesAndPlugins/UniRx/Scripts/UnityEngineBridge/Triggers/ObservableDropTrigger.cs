// for uGUI(from 4.6)
#if !(UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5)

using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Subjects;
using UnityEngine;
using UnityEngine.EventSystems; // require keep for Windows Universal App

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.UnityEngineBridge.Triggers
{
    [DisallowMultipleComponent]
    public class ObservableDropTrigger : ObservableTriggerBase, IEventSystemHandler, IDropHandler
    {
        Subject<PointerEventData> onDrop;

        void IDropHandler.OnDrop(PointerEventData eventData)
        {
            if (this.onDrop != null) this.onDrop.OnNext(eventData);
        }

        public IObservable<PointerEventData> OnDropAsObservable()
        {
            return this.onDrop ?? (this.onDrop = new Subject<PointerEventData>());
        }

        protected override void RaiseOnCompletedOnDestroy()
        {
            if (this.onDrop != null)
            {
                this.onDrop.OnCompleted();
            }
        }
    }
}


#endif