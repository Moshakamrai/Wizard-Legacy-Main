// for uGUI(from 4.6)
#if !(UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5)

using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Subjects;
using UnityEngine;
using UnityEngine.EventSystems; // require keep for Windows Universal App

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.UnityEngineBridge.Triggers
{
    [DisallowMultipleComponent]
    public class ObservablePointerUpTrigger : ObservableTriggerBase, IEventSystemHandler, IPointerUpHandler
    {
        Subject<PointerEventData> onPointerUp;

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            if (this.onPointerUp != null) this.onPointerUp.OnNext(eventData);
        }

        public IObservable<PointerEventData> OnPointerUpAsObservable()
        {
            return this.onPointerUp ?? (this.onPointerUp = new Subject<PointerEventData>());
        }

        protected override void RaiseOnCompletedOnDestroy()
        {
            if (this.onPointerUp != null)
            {
                this.onPointerUp.OnCompleted();
            }
        }
    }
}


#endif