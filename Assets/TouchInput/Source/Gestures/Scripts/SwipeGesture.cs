using System;
using TouchInput.Source.Gestures.Scripts.Contracts;
using TouchInput.Source.Managers.Scripts;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace TouchInput.Source.Gestures.Scripts
{
    public class SwipeEvent : UnityEvent<SwipeEventData> { }
    
    /// <summary>
    /// Detects swipe gestures via Unity's UI EventSystem.
    /// Any UI element rendered on top automatically blocks swipe input.
    /// Requires a Graphic with raycastTarget=true on the same GameObject.
    /// </summary>
    public class SwipeGesture : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField]
        private SwipeGestureDirection _swipeGestureDirection = SwipeGestureDirection.Vertical;
        
        [SerializeField]
        [Tooltip("Min distance in cm which should be dragged to recognize the gesture as swipe")]
        private float _minDistanceCm = 0.5f;
        
        [SerializeField]
        [Tooltip("Max time in cm given for swipe recognition when first touch happened")]
        private float _maxTimeSec = 1f;

        public SwipeGestureDirection GestureDirection
        {
            get => _swipeGestureDirection;
            set => _swipeGestureDirection = value;
        }

        public float MinDistance
        {
            get => _minDistanceCm;
            set => _minDistanceCm = value;
        }
        
        public event Action<SwipeEventData> Swiped;
        
        public SwipeEvent OnSwiped = new SwipeEvent();

        private float _dotsPerCentimeter;

        private Vector2 _startTouchPosition;
        private float _startTouchTime;

        private Vector2 _lastTouchPosition;
        private float _endTouchTime;

        private void Awake()
        {
            _dotsPerCentimeter = InputActionsManager.Instance.DotsPerCentimeter;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _startTouchPosition = eventData.position;
            _lastTouchPosition = _startTouchPosition;
            _startTouchTime = Time.realtimeSinceStartup;
        }

        public void OnDrag(PointerEventData eventData)
        {
            _lastTouchPosition = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _endTouchTime = Time.realtimeSinceStartup;
            DetectSwipe();
        }

        private void DetectSwipe()
        {
            var deltaPosition = _lastTouchPosition - _startTouchPosition;
            var deltaTime = _endTouchTime - _startTouchTime;

            if (deltaTime > _maxTimeSec)
                return;

            ModifyByDirection(ref deltaPosition);

            if (deltaPosition.magnitude < _minDistanceCm * _dotsPerCentimeter)
                return;

            var eventData = new SwipeEventData
            {
                DeltaPosition = deltaPosition,
                StartPosition = _startTouchPosition,
                EndPosition = _lastTouchPosition,
                GestureDirection = _swipeGestureDirection
            };
            
            Swiped?.Invoke(eventData);
            OnSwiped.Invoke(eventData);
        }

        private void ModifyByDirection(ref Vector2 deltaPosition)
        {
            switch (_swipeGestureDirection)
            {
                case SwipeGestureDirection.Horizontal:
                    deltaPosition.y = 0;
                    break;
                case SwipeGestureDirection.Vertical:
                    deltaPosition.x = 0;
                    break;
                case SwipeGestureDirection.Any:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(_swipeGestureDirection), _swipeGestureDirection, null);
            }
        }
    }
}
