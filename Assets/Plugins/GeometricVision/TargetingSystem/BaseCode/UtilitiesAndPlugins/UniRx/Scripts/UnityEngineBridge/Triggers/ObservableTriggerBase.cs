using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Subjects;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;
using UnityEngine; // require keep for Windows Universal App

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.UnityEngineBridge.Triggers
{
    public abstract class ObservableTriggerBase : MonoBehaviour
    {
        bool calledAwake = false;
        Subject<Unit> awake;

        /// <summary>Awake is called when the script instance is being loaded.</summary>
        void Awake()
        {
            this.calledAwake = true;
            if (this.awake != null) {
                this.awake.OnNext(Unit.Default);
                this.awake.OnCompleted(); }
        }

        /// <summary>Awake is called when the script instance is being loaded.</summary>
        public IObservable<Unit> AwakeAsObservable()
        {
            if (this.calledAwake) return Scripts.Observable.Return(Unit.Default);
            return this.awake ?? (this.awake = new Subject<Unit>());
        }

        bool calledStart = false;
        Subject<Unit> start;

        /// <summary>Start is called on the frame when a script is enabled just before any of the Update methods is called the first time.</summary>
        void Start()
        {
            this.calledStart = true;
            if (this.start != null) {
                this.start.OnNext(Unit.Default);
                this.start.OnCompleted(); }
        }

        /// <summary>Start is called on the frame when a script is enabled just before any of the Update methods is called the first time.</summary>
        public IObservable<Unit> StartAsObservable()
        {
            if (this.calledStart) return Scripts.Observable.Return(Unit.Default);
            return this.start ?? (this.start = new Subject<Unit>());
        }


        bool calledDestroy = false;
        Subject<Unit> onDestroy;

        /// <summary>This function is called when the MonoBehaviour will be destroyed.</summary>
        void OnDestroy()
        {
            this.calledDestroy = true;
            if (this.onDestroy != null) {
                this.onDestroy.OnNext(Unit.Default);
                this.onDestroy.OnCompleted(); }

            this.RaiseOnCompletedOnDestroy();
        }

        /// <summary>This function is called when the MonoBehaviour will be destroyed.</summary>
        public IObservable<Unit> OnDestroyAsObservable()
        {
            if (this == null) return Scripts.Observable.Return(Unit.Default);
            if (this.calledDestroy) return Scripts.Observable.Return(Unit.Default);
            return this.onDestroy ?? (this.onDestroy = new Subject<Unit>());
        }

        protected abstract void RaiseOnCompletedOnDestroy();
    }
}