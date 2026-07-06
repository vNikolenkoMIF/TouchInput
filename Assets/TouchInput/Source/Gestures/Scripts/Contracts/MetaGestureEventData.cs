using UnityEngine;

namespace TouchInput.Source.Gestures.Scripts.Contracts
{
    /// <summary>
    /// Snapshot of a pointer event fired by <see cref="MetaGesture"/>.
    /// PointerEventData is transient (recycled by the EventSystem), so all
    /// relevant values are copied out at the moment the event fires.
    /// </summary>
    public class MetaGestureEventData
    {
        /// <summary>Unique ID of the pointer that triggered this event (matches PointerEventData.pointerId).</summary>
        public int PointerId;

        /// <summary>Screen-space position of the pointer when the event fired.</summary>
        public Vector2 Position;

        /// <summary>Screen-space position of the pointer at the previous event for this pointer.
        /// Equals <see cref="Position"/> for <see cref="MetaGesture.PointerPressed"/> (no prior position).</summary>
        public Vector2 PreviousPosition;

        /// <summary>Screen-space movement since the previous event for this pointer.</summary>
        public Vector2 Delta;

        /// <summary>Screen-space position where this pointer was originally pressed.</summary>
        public Vector2 PressPosition;

        /// <summary>
        /// Number of pointers currently active on the gesture object at the time this event fired.
        /// For <see cref="MetaGesture.PointerReleased"/> this is the count after the releasing
        /// pointer has been removed, so the last finger gives 0.
        /// </summary>
        public int ActivePointerCount;
    }
}
