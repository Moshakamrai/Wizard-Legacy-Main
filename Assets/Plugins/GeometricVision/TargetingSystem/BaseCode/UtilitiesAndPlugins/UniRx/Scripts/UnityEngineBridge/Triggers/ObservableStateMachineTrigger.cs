// over Unity5 added StateMachineBehaviour
#if !(UNITY_4_7 || UNITY_4_6 || UNITY_4_5 || UNITY_4_4 || UNITY_4_3 || UNITY_4_2 || UNITY_4_1 || UNITY_4_0_1 || UNITY_4_0 || UNITY_3_5 || UNITY_3_4 || UNITY_3_3 || UNITY_3_2 || UNITY_3_1 || UNITY_3_0_0 || UNITY_3_0 || UNITY_2_6_1 || UNITY_2_6)

using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Subjects;
using UnityEngine; // require keep for Windows Universal App

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.UnityEngineBridge.Triggers
{
    [DisallowMultipleComponent]
    public class ObservableStateMachineTrigger : StateMachineBehaviour
    {
        public class OnStateInfo
        {
            public Animator Animator { get; private set; }
            public AnimatorStateInfo StateInfo { get; private set; }
            public int LayerIndex { get; private set; }

            public OnStateInfo(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
            {
                this.Animator = animator;
                this.StateInfo = stateInfo;
                this.LayerIndex = layerIndex;
            }
        }

        public class OnStateMachineInfo
        {
            public Animator Animator { get; private set; }
            public int StateMachinePathHash { get; private set; }

            public OnStateMachineInfo(Animator animator, int stateMachinePathHash)
            {
                this.Animator = animator;
                this.StateMachinePathHash = stateMachinePathHash;
            }
        }

        // OnStateExit

        Subject<OnStateInfo> onStateExit;

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (this.onStateExit != null) this.onStateExit.OnNext(new OnStateInfo(animator, stateInfo, layerIndex));
        }

        public IObservable<OnStateInfo> OnStateExitAsObservable()
        {
            return this.onStateExit ?? (this.onStateExit = new Subject<OnStateInfo>());
        }

        // OnStateEnter

        Subject<OnStateInfo> onStateEnter;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (this.onStateEnter != null) this.onStateEnter.OnNext(new OnStateInfo(animator, stateInfo, layerIndex));
        }

        public IObservable<OnStateInfo> OnStateEnterAsObservable()
        {
            return this.onStateEnter ?? (this.onStateEnter = new Subject<OnStateInfo>());
        }

        // OnStateIK

        Subject<OnStateInfo> onStateIK;

        public override void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if(this.onStateIK !=null) this.onStateIK.OnNext(new OnStateInfo(animator, stateInfo, layerIndex));
        }

        public IObservable<OnStateInfo> OnStateIKAsObservable()
        {
            return this.onStateIK ?? (this.onStateIK = new Subject<OnStateInfo>());
        }

        // Does not implments OnStateMove.
        // ObservableStateMachine Trigger makes stop animating.
        // By defining OnAnimatorMove, you are signifying that you want to intercept the movement of the root object and apply it yourself.
        // http://fogbugz.unity3d.com/default.asp?700990_9jqaim4ev33i8e9h

        //// OnStateMove

        //Subject<OnStateInfo> onStateMove;

        //public override void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        //{
        //    if (onStateMove != null) onStateMove.OnNext(new OnStateInfo(animator, stateInfo, layerIndex));
        //}

        //public IObservable<OnStateInfo> OnStateMoveAsObservable()
        //{
        //    return onStateMove ?? (onStateMove = new Subject<OnStateInfo>());
        //}

        // OnStateUpdate

        Subject<OnStateInfo> onStateUpdate;

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (this.onStateUpdate != null) this.onStateUpdate.OnNext(new OnStateInfo(animator, stateInfo, layerIndex));
        }

        public IObservable<OnStateInfo> OnStateUpdateAsObservable()
        {
            return this.onStateUpdate ?? (this.onStateUpdate = new Subject<OnStateInfo>());
        }

        // OnStateMachineEnter

        Subject<OnStateMachineInfo> onStateMachineEnter;

        public override void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
        {
            if (this.onStateMachineEnter != null) this.onStateMachineEnter.OnNext(new OnStateMachineInfo(animator, stateMachinePathHash));
        }

        public IObservable<OnStateMachineInfo> OnStateMachineEnterAsObservable()
        {
            return this.onStateMachineEnter ?? (this.onStateMachineEnter = new Subject<OnStateMachineInfo>());
        }

        // OnStateMachineExit

        Subject<OnStateMachineInfo> onStateMachineExit;

        public override void OnStateMachineExit(Animator animator, int stateMachinePathHash)
        {
            if (this.onStateMachineExit != null) this.onStateMachineExit.OnNext(new OnStateMachineInfo(animator, stateMachinePathHash));
        }

        public IObservable<OnStateMachineInfo> OnStateMachineExitAsObservable()
        {
            return this.onStateMachineExit ?? (this.onStateMachineExit = new Subject<OnStateMachineInfo>());
        }
    }
}

#endif