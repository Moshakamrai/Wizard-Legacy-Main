// for uGUI(from 4.6)
#if !(UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5)

using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Subjects;
using UnityEngine;
using UnityEngine.EventSystems; // require keep for Windows Universal App

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.UnityEngineBridge.Triggers
{
    [DisallowMultipleComponent]
    public class ObservableBeginDragTrigger : ObservableTriggerBase, IEventSystemHandler, IBeginDragHandler
    {
        Subject<PointerEventData> onBeginDrag;

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            if (this.onBeginDrag != null) this.onBeginDrag.OnNext(eventData);
        }

        public IObservable<PointerEventData> OnBeginDragAsObservable()
        {
            return this.onBeginDrag ?? (this.onBeginDrag = new Subject<PointerEventData>());
        }

        protected override void RaiseOnCompletedOnDestroy()
        {
            if (this.onBeginDrag != null)
            {
                this.onBeginDrag.OnCompleted();
            }
        }
    }
}


#endif