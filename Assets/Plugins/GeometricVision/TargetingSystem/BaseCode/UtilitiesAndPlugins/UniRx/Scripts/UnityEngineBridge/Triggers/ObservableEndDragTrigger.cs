// for uGUI(from 4.6)
#if !(UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5)

using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Subjects;
using UnityEngine;
using UnityEngine.EventSystems; // require keep for Windows Universal App

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.UnityEngineBridge.Triggers
{
    [DisallowMultipleComponent]
    public class ObservableEndDragTrigger : ObservableTriggerBase, IEventSystemHandler, IEndDragHandler
    {
        Subject<PointerEventData> onEndDrag;

        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            if (this.onEndDrag != null) this.onEndDrag.OnNext(eventData);
        }

        public IObservable<PointerEventData> OnEndDragAsObservable()
        {
            return this.onEndDrag ?? (this.onEndDrag = new Subject<PointerEventData>());
        }

        protected override void RaiseOnCompletedOnDestroy()
        {
            if (this.onEndDrag != null)
            {
                this.onEndDrag.OnCompleted();
            }
        }
    }
}


#endif