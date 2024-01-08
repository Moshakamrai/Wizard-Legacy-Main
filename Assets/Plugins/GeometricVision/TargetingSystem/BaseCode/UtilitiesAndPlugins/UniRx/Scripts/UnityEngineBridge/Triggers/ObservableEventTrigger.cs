// for uGUI(from 4.6)
#if !(UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5)

using System;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.Subjects;
using UnityEngine;
using UnityEngine.EventSystems; // require keep for Windows Universal App

namespace Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.UnityEngineBridge.Triggers
{
    [DisallowMultipleComponent]
    public class ObservableEventTrigger : ObservableTriggerBase, IEventSystemHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IBeginDragHandler, IInitializePotentialDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IScrollHandler, IUpdateSelectedHandler, ISelectHandler, IDeselectHandler, IMoveHandler, ISubmitHandler, ICancelHandler
    {
        #region IDeselectHandler

        Subject<BaseEventData> onDeselect;

        void IDeselectHandler.OnDeselect(BaseEventData eventData)
        {
            if (this.onDeselect != null) this.onDeselect.OnNext(eventData);
        }

        public IObservable<BaseEventData> OnDeselectAsObservable()
        {
            return this.onDeselect ?? (this.onDeselect = new Subject<BaseEventData>());
        }

        #endregion

        #region IMoveHandler

        Subject<AxisEventData> onMove;

        void IMoveHandler.OnMove(AxisEventData eventData)
        {
            if (this.onMove != null) this.onMove.OnNext(eventData);
        }

        public IObservable<AxisEventData> OnMoveAsObservable()
        {
            return this.onMove ?? (this.onMove = new Subject<AxisEventData>());
        }

        #endregion

        #region IPointerDownHandler

        Subject<PointerEventData> onPointerDown;

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            if (this.onPointerDown != null) this.onPointerDown.OnNext(eventData);
        }

        public IObservable<PointerEventData> OnPointerDownAsObservable()
        {
            return this.onPointerDown ?? (this.onPointerDown = new Subject<PointerEventData>());
        }

        #endregion

        #region IPointerEnterHandler

        Subject<PointerEventData> onPointerEnter;

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            if (this.onPointerEnter != null) this.onPointerEnter.OnNext(eventData);
        }

        public IObservable<PointerEventData> OnPointerEnterAsObservable()
        {
            return this.onPointerEnter ?? (this.onPointerEnter = new Subject<PointerEventData>());
        }

        #endregion

        #region IPointerExitHandler

        Subject<PointerEventData> onPointerExit;

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            if (this.onPointerExit != null) this.onPointerExit.OnNext(eventData);
        }

        public IObservable<PointerEventData> OnPointerExitAsObservable()
        {
            return this.onPointerExit ?? (this.onPointerExit = new Subject<PointerEventData>());
        }

        #endregion

        #region IPointerUpHandler

        Subject<PointerEventData> onPointerUp;

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            if (this.onPointerUp != null) this.onPointerUp.OnNext(eventData);
        }

        public IObservable<PointerEventData> OnPointerUpAsObservable()
        {
            return this.onPointerUp ?? (this.onPointerUp = new Subject<PointerEventData>());
        }

        #endregion

        #region ISelectHandler

        Subject<BaseEventData> onSelect;

        void ISelectHandler.OnSelect(BaseEventData eventData)
        {
            if (this.onSelect != null) this.onSelect.OnNext(eventData);
        }

        public IObservable<BaseEventData> OnSelectAsObservable()
        {
            return this.onSelect ?? (this.onSelect = new Subject<BaseEventData>());
        }

        #endregion

        #region IPointerClickHandler

        Subject<PointerEventData> onPointerClick;

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            if (this.onPointerClick != null) this.onPointerClick.OnNext(eventData);
        }

        public IObservable<PointerEventData> OnPointerClickAsObservable()
        {
            return this.onPointerClick ?? (this.onPointerClick = new Subject<PointerEventData>());
        }

        #endregion

        #region ISubmitHandler

        Subject<BaseEventData> onSubmit;

        void ISubmitHandler.OnSubmit(BaseEventData eventData)
        {
            if (this.onSubmit != null) this.onSubmit.OnNext(eventData);
        }

        public IObservable<BaseEventData> OnSubmitAsObservable()
        {
            return this.onSubmit ?? (this.onSubmit = new Subject<BaseEventData>());
        }

        #endregion

        #region IDragHandler

        Subject<PointerEventData> onDrag;

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            if (this.onDrag != null) this.onDrag.OnNext(eventData);
        }

        public IObservable<PointerEventData> OnDragAsObservable()
        {
            return this.onDrag ?? (this.onDrag = new Subject<PointerEventData>());
        }

        #endregion

        #region IBeginDragHandler

        Subject<PointerEventData> onBeginDrag;

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            if (this.onBeginDrag != null) this.onBeginDrag.OnNext(eventData);
        }

        public IObservable<PointerEventData> OnBeginDragAsObservable()
        {
            return this.onBeginDrag ?? (this.onBeginDrag = new Subject<PointerEventData>());
        }

        #endregion

        #region IEndDragHandler

        Subject<PointerEventData> onEndDrag;

        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            if (this.onEndDrag != null) this.onEndDrag.OnNext(eventData);
        }

        public IObservable<PointerEventData> OnEndDragAsObservable()
        {
            return this.onEndDrag ?? (this.onEndDrag = new Subject<PointerEventData>());
        }

        #endregion

        #region IDropHandler

        Subject<PointerEventData> onDrop;

        void IDropHandler.OnDrop(PointerEventData eventData)
        {
            if (this.onDrop != null) this.onDrop.OnNext(eventData);
        }

        public IObservable<PointerEventData> OnDropAsObservable()
        {
            return this.onDrop ?? (this.onDrop = new Subject<PointerEventData>());
        }

        #endregion

        #region IUpdateSelectedHandler

        Subject<BaseEventData> onUpdateSelected;

        void IUpdateSelectedHandler.OnUpdateSelected(BaseEventData eventData)
        {
            if (this.onUpdateSelected != null) this.onUpdateSelected.OnNext(eventData);
        }

        public IObservable<BaseEventData> OnUpdateSelectedAsObservable()
        {
            return this.onUpdateSelected ?? (this.onUpdateSelected = new Subject<BaseEventData>());
        }

        #endregion

        #region IInitializePotentialDragHandler

        Subject<PointerEventData> onInitializePotentialDrag;

        void IInitializePotentialDragHandler.OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (this.onInitializePotentialDrag != null) this.onInitializePotentialDrag.OnNext(eventData);
        }

        public IObservable<PointerEventData> OnInitializePotentialDragAsObservable()
        {
            return this.onInitializePotentialDrag ?? (this.onInitializePotentialDrag = new Subject<PointerEventData>());
        }

        #endregion

        #region ICancelHandler

        Subject<BaseEventData> onCancel;

        void ICancelHandler.OnCancel(BaseEventData eventData)
        {
            if (this.onCancel != null) this.onCancel.OnNext(eventData);
        }

        public IObservable<BaseEventData> OnCancelAsObservable()
        {
            return this.onCancel ?? (this.onCancel = new Subject<BaseEventData>());
        }

        #endregion

        #region IScrollHandler

        Subject<PointerEventData> onScroll;

        void IScrollHandler.OnScroll(PointerEventData eventData)
        {
            if (this.onScroll != null) this.onScroll.OnNext(eventData);
        }

        public IObservable<PointerEventData> OnScrollAsObservable()
        {
            return this.onScroll ?? (this.onScroll = new Subject<PointerEventData>());
        }

        #endregion

        protected override void RaiseOnCompletedOnDestroy()
        {
            if (this.onDeselect != null)
            {
                this.onDeselect.OnCompleted();
            }
            if (this.onMove != null)
            {
                this.onMove.OnCompleted();
            }
            if (this.onPointerDown != null)
            {
                this.onPointerDown.OnCompleted();
            }
            if (this.onPointerEnter != null)
            {
                this.onPointerEnter.OnCompleted();
            }
            if (this.onPointerExit != null)
            {
                this.onPointerExit.OnCompleted();
            }
            if (this.onPointerUp != null)
            {
                this.onPointerUp.OnCompleted();
            }
            if (this.onSelect != null)
            {
                this.onSelect.OnCompleted();
            }
            if (this.onPointerClick != null)
            {
                this.onPointerClick.OnCompleted();
            }
            if (this.onSubmit != null)
            {
                this.onSubmit.OnCompleted();
            }
            if (this.onDrag != null)
            {
                this.onDrag.OnCompleted();
            }
            if (this.onBeginDrag != null)
            {
                this.onBeginDrag.OnCompleted();
            }
            if (this.onEndDrag != null)
            {
                this.onEndDrag.OnCompleted();
            }
            if (this.onDrop != null)
            {
                this.onDrop.OnCompleted();
            }
            if (this.onUpdateSelected != null)
            {
                this.onUpdateSelected.OnCompleted();
            }
            if (this.onInitializePotentialDrag != null)
            {
                this.onInitializePotentialDrag.OnCompleted();
            }
            if (this.onCancel != null)
            {
                this.onCancel.OnCompleted();
            }
            if (this.onScroll != null)
            {
                this.onScroll.OnCompleted();
            }
        }
    }
}

#endif