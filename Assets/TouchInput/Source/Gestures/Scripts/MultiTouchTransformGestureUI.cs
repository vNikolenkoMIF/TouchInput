using TouchInput.Source.Gestures.Scripts.Modifiers;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TouchInput.Source.Gestures.Scripts
{
    /// <summary>
    /// EventSystem-backed gesture recognizer. Implements <see cref="IBeginDragHandler"/>,
    /// <see cref="IDragHandler"/>, and <see cref="IEndDragHandler"/> and forwards pointer
    /// positions to <see cref="MultiTouchTransformGestureBase"/>.
    ///
    /// In the Editor, right-click simulates a second touch via <see cref="SimulateSecondTouchAsOppositePosition"/>.
    ///
    /// Requires a Graphic (e.g. transparent Image with raycastTarget=true) on the same
    /// GameObject so the EventSystem can dispatch pointer events here.
    /// </summary>
    public sealed class MultiTouchTransformGestureUI : MultiTouchTransformGestureBase,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private IDragPositionModifier _modifier;

        protected override void Awake()
        {
            base.Awake();

            _modifier = SystemInfo.deviceType == DeviceType.Handheld 
                ? new DragScreenPosition() 
                : new SimulateSecondTouchAsOppositePosition();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            var pos = _modifier.GetBeginPosition(eventData.pointerId, eventData.position);
            BeginPointer(eventData.pointerId, pos);
        }

        public void OnDrag(PointerEventData eventData)
        {
            var pos = _modifier.GetDragPosition(eventData.pointerId, eventData.position);
            MovePointer(eventData.pointerId, pos);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _modifier.EndDrag(eventData.pointerId, eventData.position);
            EndPointer(eventData.pointerId);
        }
    }
}
