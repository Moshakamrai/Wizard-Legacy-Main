using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Subjects;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;
using UnityEngine; // require keep for Windows Universal App

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.UnityEngineBridge.Triggers
{
    [DisallowMultipleComponent]
    public class ObservableParticleTrigger : ObservableTriggerBase
    {
        Subject<GameObject> onParticleCollision;
#if UNITY_5_4_OR_NEWER
        Subject<Unit> onParticleTrigger;
#endif

        /// <summary>OnParticleCollision is called when a particle hits a collider.</summary>
        void OnParticleCollision(GameObject other)
        {
            if (this.onParticleCollision != null) this.onParticleCollision.OnNext(other);
        }

        /// <summary>OnParticleCollision is called when a particle hits a collider.</summary>
        public IObservable<GameObject> OnParticleCollisionAsObservable()
        {
            return this.onParticleCollision ?? (this.onParticleCollision = new Subject<GameObject>());
        }

#if UNITY_5_4_OR_NEWER

        /// <summary>OnParticleTrigger is called when any particles in a particle system meet the conditions in the trigger module.</summary>
        void OnParticleTrigger()
        {
            if (this.onParticleTrigger != null) this.onParticleTrigger.OnNext(Unit.Default);
        }

        /// <summary>OnParticleTrigger is called when any particles in a particle system meet the conditions in the trigger module.</summary>
        public IObservable<Unit> OnParticleTriggerAsObservable()
        {
            return this.onParticleTrigger ?? (this.onParticleTrigger = new Subject<Unit>());
        }

#endif

        protected override void RaiseOnCompletedOnDestroy()
        {
            if (this.onParticleCollision != null)
            {
                this.onParticleCollision.OnCompleted();
            }
#if UNITY_5_4_OR_NEWER
            if (this.onParticleTrigger != null)
            {
                this.onParticleTrigger.OnCompleted();
            }
#endif
        }
    }
}