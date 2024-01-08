// for uGUI(from 4.6)
#if !(UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5)

using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Subjects;
using UnityEngine;
using UnityEngine.EventSystems; // require keep for Windows Universal App

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.UnityEngineBridge.Triggers
{
    [DisallowMultipleComponent]
    public class ObservableScrollTrigger : ObservableTriggerBase, IEventSystemHandler, IScrollHandler
    {
        Subject<PointerEventData> onScroll;

        void IScrollHandler.OnScroll(PointerEventData eventData)
        {
            if (this.onScroll != null) this.onScroll.OnNext(eventData);
        }

        public IObservable<PointerEventData> OnScrollAsObservable()
        {
            return this.onScroll ?? (this.onScroll = new Subject<PointerEventData>());
        }

        protected override void RaiseOnCompletedOnDestroy()
        {
            if (this.onScroll != null)
            {
                this.onScroll.OnCompleted();
            }
        }
    }
}


#endif