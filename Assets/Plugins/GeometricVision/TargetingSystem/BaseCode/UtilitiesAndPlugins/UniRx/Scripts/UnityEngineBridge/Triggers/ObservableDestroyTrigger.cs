using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Disposables;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Subjects;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;
using UnityEngine; // require keep for Windows Universal App

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.UnityEngineBridge.Triggers
{
    [DisallowMultipleComponent]
    public class ObservableDestroyTrigger : MonoBehaviour
    {
        bool calledDestroy = false;
        Subject<Unit> onDestroy;
        CompositeDisposable disposablesOnDestroy;

        [Obsolete("Internal Use.")]
        internal bool IsMonitoredActivate { get; set; }

        public bool IsActivated { get; private set; }

        /// <summary>
        /// Check called OnDestroy.
        /// This property does not guarantees GameObject was destroyed,
        /// when gameObject is deactive, does not raise OnDestroy.
        /// </summary>
        public bool IsCalledOnDestroy { get { return this.calledDestroy; } }

        void Awake()
        {
            this.IsActivated = true;
        }

        /// <summary>This function is called when the MonoBehaviour will be destroyed.</summary>
        void OnDestroy()
        {
            if (!this.calledDestroy)
            {
                this.calledDestroy = true;
                if (this.disposablesOnDestroy != null) this.disposablesOnDestroy.Dispose();
                if (this.onDestroy != null) {
                    this.onDestroy.OnNext(Unit.Default);
                    this.onDestroy.OnCompleted(); }
            }
        }

        /// <summary>This function is called when the MonoBehaviour will be destroyed.</summary>
        public IObservable<Unit> OnDestroyAsObservable()
        {
            if (this == null) return Scripts.Observable.Return(Unit.Default);
            if (this.calledDestroy) return Scripts.Observable.Return(Unit.Default);
            return this.onDestroy ?? (this.onDestroy = new Subject<Unit>());
        }

        /// <summary>Invoke OnDestroy, this method is used on internal.</summary>
        public void ForceRaiseOnDestroy()
        {
            this.OnDestroy();
        }

        public void AddDisposableOnDestroy(IDisposable disposable)
        {
            if (this.calledDestroy)
            {
                disposable.Dispose();
                return;
            }

            if (this.disposablesOnDestroy == null) this.disposablesOnDestroy = new CompositeDisposable();
            this.disposablesOnDestroy.Add(disposable);
        }
    }
}