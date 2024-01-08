// for uGUI(from 4.6)
#if !(UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5)

using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Subjects;
using UnityEngine;
using UnityEngine.EventSystems; // require keep for Windows Universal App

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.UnityEngineBridge.Triggers
{
    [DisallowMultipleComponent]
    public class ObservableMoveTrigger : ObservableTriggerBase, IEventSystemHandler, IMoveHandler
    {
        Subject<AxisEventData> onMove;

        void IMoveHandler.OnMove(AxisEventData eventData)
        {
            if (this.onMove != null) this.onMove.OnNext(eventData);
        }

        public IObservable<AxisEventData> OnMoveAsObservable()
        {
            return this.onMove ?? (this.onMove = new Subject<AxisEventData>());
        }

        protected override void RaiseOnCompletedOnDestroy()
        {
            if (this.onMove != null)
            {
                this.onMove.OnCompleted();
            }
        }
    }
}


#endif