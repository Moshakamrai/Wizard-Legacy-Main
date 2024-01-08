#if !(UNITY_IPHONE || UNITY_ANDROID || UNITY_METRO)

using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Subjects;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.System;
using UnityEngine; // require keep for Windows Universal App

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.UnityEngineBridge.Triggers
{
    [DisallowMultipleComponent]
    public class ObservableMouseTrigger : ObservableTriggerBase
    {
        Subject<Unit> onMouseDown;

        /// <summary>OnMouseDown is called when the user has pressed the mouse button while over the GUIElement or Collider.</summary>
         void OnMouseDown()
        {
            if (this.onMouseDown != null) this.onMouseDown.OnNext(Unit.Default);
        }

        /// <summary>OnMouseDown is called when the user has pressed the mouse button while over the GUIElement or Collider.</summary>
        public IObservable<Unit> OnMouseDownAsObservable()
        {
            return this.onMouseDown ?? (this.onMouseDown = new Subject<Unit>());
        }

        Subject<Unit> onMouseDrag;

        /// <summary>OnMouseDrag is called when the user has clicked on a GUIElement or Collider and is still holding down the mouse.</summary>
         void OnMouseDrag()
        {
            if (this.onMouseDrag != null) this.onMouseDrag.OnNext(Unit.Default);
        }

        /// <summary>OnMouseDrag is called when the user has clicked on a GUIElement or Collider and is still holding down the mouse.</summary>
        public IObservable<Unit> OnMouseDragAsObservable()
        {
            return this.onMouseDrag ?? (this.onMouseDrag = new Subject<Unit>());
        }

        Subject<Unit> onMouseEnter;

        /// <summary>OnMouseEnter is called when the mouse entered the GUIElement or Collider.</summary>
         void OnMouseEnter()
        {
            if (this.onMouseEnter != null) this.onMouseEnter.OnNext(Unit.Default);
        }

        /// <summary>OnMouseEnter is called when the mouse entered the GUIElement or Collider.</summary>
        public IObservable<Unit> OnMouseEnterAsObservable()
        {
            return this.onMouseEnter ?? (this.onMouseEnter = new Subject<Unit>());
        }

        Subject<Unit> onMouseExit;

        /// <summary>OnMouseExit is called when the mouse is not any longer over the GUIElement or Collider.</summary>
         void OnMouseExit()
        {
            if (this.onMouseExit != null) this.onMouseExit.OnNext(Unit.Default);
        }

        /// <summary>OnMouseExit is called when the mouse is not any longer over the GUIElement or Collider.</summary>
        public IObservable<Unit> OnMouseExitAsObservable()
        {
            return this.onMouseExit ?? (this.onMouseExit = new Subject<Unit>());
        }

        Subject<Unit> onMouseOver;

        /// <summary>OnMouseOver is called every frame while the mouse is over the GUIElement or Collider.</summary>
         void OnMouseOver()
        {
            if (this.onMouseOver != null) this.onMouseOver.OnNext(Unit.Default);
        }

        /// <summary>OnMouseOver is called every frame while the mouse is over the GUIElement or Collider.</summary>
        public IObservable<Unit> OnMouseOverAsObservable()
        {
            return this.onMouseOver ?? (this.onMouseOver = new Subject<Unit>());
        }

        Subject<Unit> onMouseUp;

        /// <summary>OnMouseUp is called when the user has released the mouse button.</summary>
         void OnMouseUp()
        {
            if (this.onMouseUp != null) this.onMouseUp.OnNext(Unit.Default);
        }

        /// <summary>OnMouseUp is called when the user has released the mouse button.</summary>
        public IObservable<Unit> OnMouseUpAsObservable()
        {
            return this.onMouseUp ?? (this.onMouseUp = new Subject<Unit>());
        }

        Subject<Unit> onMouseUpAsButton;

        /// <summary>OnMouseUpAsButton is only called when the mouse is released over the same GUIElement or Collider as it was pressed.</summary>
         void OnMouseUpAsButton()
        {
            if (this.onMouseUpAsButton != null) this.onMouseUpAsButton.OnNext(Unit.Default);
        }

        /// <summary>OnMouseUpAsButton is only called when the mouse is released over the same GUIElement or Collider as it was pressed.</summary>
        public IObservable<Unit> OnMouseUpAsButtonAsObservable()
        {
            return this.onMouseUpAsButton ?? (this.onMouseUpAsButton = new Subject<Unit>());
        }

        protected override void RaiseOnCompletedOnDestroy()
        {
            if (this.onMouseDown != null)
            {
                this.onMouseDown.OnCompleted();
            }
            if (this.onMouseDrag != null)
            {
                this.onMouseDrag.OnCompleted();
            }
            if (this.onMouseEnter != null)
            {
                this.onMouseEnter.OnCompleted();
            }
            if (this.onMouseExit != null)
            {
                this.onMouseExit.OnCompleted();
            }
            if (this.onMouseOver != null)
            {
                this.onMouseOver.OnCompleted();
            }
            if (this.onMouseUp != null)
            {
                this.onMouseUp.OnCompleted();
            }
            if (this.onMouseUpAsButton != null)
            {
                this.onMouseUpAsButton.OnCompleted();
            }
        }
    }
}

#endif