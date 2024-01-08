// for uGUI(from 4.6)
#if !(UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5)

using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Subjects;
using UnityEngine;
using UnityEngine.EventSystems; // require keep for Windows Universal App

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.UnityEngineBridge.Triggers
{
    [DisallowMultipleComponent]
    public class ObservablePointerClickTrigger : ObservableTriggerBase, IEventSystemHandler, IPointerClickHandler
    {
        Subject<PointerEventData> onPointerClick;

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            if (this.onPointerClick != null) this.onPointerClick.OnNext(eventData);
        }

        public IObservable<PointerEventData> OnPointerClickAsObservable()
        {
            return this.onPointerClick ?? (this.onPointerClick = new Subject<PointerEventData>());
        }

        protected override void RaiseOnCompletedOnDestroy()
        {
            if (this.onPointerClick != null)
            {
                this.onPointerClick.OnCompleted();
            }
        }
    }
}


#endif