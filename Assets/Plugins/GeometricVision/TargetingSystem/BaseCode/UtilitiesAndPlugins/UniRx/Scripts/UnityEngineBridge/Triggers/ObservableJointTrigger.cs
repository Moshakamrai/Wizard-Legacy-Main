using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Subjects;
using UnityEngine; // require keep for Windows Universal App

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.UnityEngineBridge.Triggers
{
    [DisallowMultipleComponent]
    public class ObservableJointTrigger : ObservableTriggerBase
    {
        Subject<float> onJointBreak;

        void OnJointBreak(float breakForce)
        {
            if (this.onJointBreak != null) this.onJointBreak.OnNext(breakForce);
        }

        public IObservable<float> OnJointBreakAsObservable()
        {
            return this.onJointBreak ?? (this.onJointBreak = new Subject<float>());
        }
        
        
        Subject<Joint2D> onJointBreak2D;

        void OnJointBreak2D(Joint2D brokenJoint)
        {
            if (this.onJointBreak2D != null) this.onJointBreak2D.OnNext(brokenJoint);
        }

        public IObservable<Joint2D> OnJointBreak2DAsObservable()
        {
            return this.onJointBreak2D ?? (this.onJointBreak2D = new Subject<Joint2D>());
        }
        

        protected override void RaiseOnCompletedOnDestroy()
        {
            if (this.onJointBreak != null)
            {
                this.onJointBreak.OnCompleted();
            }
            if (this.onJointBreak2D != null)
            {
                this.onJointBreak2D.OnCompleted();
            }
        }
    }
}