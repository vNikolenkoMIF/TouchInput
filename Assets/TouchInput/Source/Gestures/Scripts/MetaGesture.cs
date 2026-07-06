using System;
using System.Collections.Generic;
using TouchInput.Source.Gestures.Scripts.Contracts;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TouchInput.Source.Gestures.Scripts
{
    [Serializable]
    public class MetaGestureEvent : UnityEvent<MetaGestureEventData> {}

    /// <summary>
    /// Aggregates all pointer/touch events on this object and re-emits them as unified events,
    /// analogous to TouchScript's MetaGesture. All active pointers are tracked simultaneously,
    /// so listeners always know the current touch count via <see cref="MetaGestureEventData.ActivePointerCount"/>.
    ///
    /// <list type="bullet">
    ///   <item><see cref="PointerPressed"/>  — any pointer pressed on this object.</item>
    ///   <item><see cref="PointerUpdated"/>  — any active pointer moved (past EventSystem drag threshold).</item>
    ///   <item><see cref="PointerReleased"/> — any pointer released from this object.</item>
    /// </list>
    ///
    /// Requires a Graphic with raycastTarget=true on the same GameObject so the EventSystem
    /// can dispatch pointer events here.
    /// </summary>
    public class MetaGesture : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public InputActionPhase CurrentPhase { get; private set; } = InputActionPhase.Waiting;

        public MetaGestureEvent OnPointerPressed  = new();
        public MetaGestureEvent OnPointerUpdated  = new();
        public MetaGestureEvent OnPointerReleased = new();

        public event Action<MetaGestureEventData> PointerPressed;
        public event Action<MetaGestureEventData> PointerUpdated;
        public event Action<MetaGestureEventData> PointerReleased;

        /// <summary>
        /// Fires whenever <see cref="CurrentPhase"/> transitions:
        /// Started (first press), Performed (any drag), Canceled (last release).
        /// </summary>
        public event Action<InputActionPhase> PhaseChanged;

        // Maps pointerId → current position.
        private readonly Dictionary<int, Vector2> _currentPositions = new();
        // Maps pointerId → position at the previous event, for PreviousPosition in event data.
        private readonly Dictionary<int, Vector2> _previousPositions = new();

        public void OnPointerDown(PointerEventData eventData)
        {
            var id = eventData.pointerId;
            _previousPositions[id] = eventData.position; // no prior position — equals current
            _currentPositions[id]  = eventData.position;
            
            Fire(PointerPressed, OnPointerPressed, eventData);
            SetPhase(InputActionPhase.Started);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Guard: ensure the pointer is tracked if OnPointerDown was missed (e.g. drag
            // started outside this object and moved in), then let OnDrag carry the event.
            var id = eventData.pointerId;
            
            _previousPositions.TryAdd(id, eventData.position);
            _currentPositions.TryAdd(id, eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            var id = eventData.pointerId;
            _previousPositions[id] = _currentPositions.GetValueOrDefault(id, eventData.position);
            _currentPositions[id]  = eventData.position;
            
            Fire(PointerUpdated, OnPointerUpdated, eventData);
            SetPhase(InputActionPhase.Performed);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            // OnPointerUp fires immediately after OnEndDrag and handles removal + PointerReleased.
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            var id = eventData.pointerId;
            _previousPositions[id] = _currentPositions.GetValueOrDefault(id, eventData.position);
            _currentPositions.Remove(id);
            // ActivePointerCount reflects remaining pointers after this one is removed,
            // so the last finger gives 0.
            Fire(PointerReleased, OnPointerReleased, eventData);
            _previousPositions.Remove(id);

            // Only transition to Canceled when all pointers are gone.
            if (_currentPositions.Count == 0)
                SetPhase(InputActionPhase.Canceled);
        }

        private void Fire(
            Action<MetaGestureEventData> csEvent,
            MetaGestureEvent unityEvent,
            PointerEventData source)
        {
            var id = source.pointerId;
            var data = new MetaGestureEventData
            {
                PointerId          = id,
                Position           = source.position,
                PreviousPosition   = _previousPositions.GetValueOrDefault(id, source.position),
                Delta              = source.delta,
                PressPosition      = source.pressPosition,
                ActivePointerCount = _currentPositions.Count,
            };

            csEvent?.Invoke(data);
            unityEvent.Invoke(data);
        }

        private void SetPhase(InputActionPhase phase)
        {
            if (CurrentPhase == phase) return;
            CurrentPhase = phase;
            PhaseChanged?.Invoke(phase);
        }
    }
}
