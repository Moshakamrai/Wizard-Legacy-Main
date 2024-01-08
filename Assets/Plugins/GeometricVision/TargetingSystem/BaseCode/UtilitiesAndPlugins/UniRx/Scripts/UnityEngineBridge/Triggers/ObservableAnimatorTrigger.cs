using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Subjects;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;
using UnityEngine; // require keep for Windows Universal App

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.UnityEngineBridge.Triggers
{
    [DisallowMultipleComponent]
    public class ObservableAnimatorTrigger : ObservableTriggerBase
    {
        Subject<int> onAnimatorIK;

        /// <summary>Callback for setting up animation IK (inverse kinematics).</summary>
        void OnAnimatorIK(int layerIndex)
        {
            if (this.onAnimatorIK != null) this.onAnimatorIK.OnNext(layerIndex);
        }

        /// <summary>Callback for setting up animation IK (inverse kinematics).</summary>
        public IObservable<int> OnAnimatorIKAsObservable()
        {
            return this.onAnimatorIK ?? (this.onAnimatorIK = new Subject<int>());
        }

        Subject<Unit> onAnimatorMove;

        /// <summary>Callback for processing animation movements for modifying root motion.</summary>
        void OnAnimatorMove()
        {
            if (this.onAnimatorMove != null) this.onAnimatorMove.OnNext(Unit.Default);
        }

        /// <summary>Callback for processing animation movements for modifying root motion.</summary>
        public IObservable<Unit> OnAnimatorMoveAsObservable()
        {
            return this.onAnimatorMove ?? (this.onAnimatorMove = new Subject<Unit>());
        }

        protected override void RaiseOnCompletedOnDestroy()
        {
            if (this.onAnimatorIK != null)
            {
                this.onAnimatorIK.OnCompleted();
            }
            if (this.onAnimatorMove != null)
            {
                this.onAnimatorMove.OnCompleted();
            }
        }
    }
}