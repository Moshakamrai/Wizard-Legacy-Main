// for uGUI(from 4.6)
#if !(UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5)

using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Subjects;
using UnityEngine;
using UnityEngine.EventSystems; // require keep for Windows Universal App

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.UnityEngineBridge.Triggers
{
    [DisallowMultipleComponent]
    public class ObservableSubmitTrigger : ObservableTriggerBase, IEventSystemHandler, ISubmitHandler
    {
        Subject<BaseEventData> onSubmit;

        void ISubmitHandler.OnSubmit(BaseEventData eventData)
        {
            if (this.onSubmit != null) this.onSubmit.OnNext(eventData);
        }

        public IObservable<BaseEventData> OnSubmitAsObservable()
        {
            return this.onSubmit ?? (this.onSubmit = new Subject<BaseEventData>());
        }

        protected override void RaiseOnCompletedOnDestroy()
        {
            if (this.onSubmit != null)
            {
                this.onSubmit.OnCompleted();
            }
        }
    }
}


#endif