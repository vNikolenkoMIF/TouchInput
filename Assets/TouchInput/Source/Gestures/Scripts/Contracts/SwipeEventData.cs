using UnityEngine;

namespace TouchInput.Source.Gestures.Scripts.Contracts
{
    public class SwipeEventData
    {
        public SwipeGestureDirection GestureDirection;
        
        public Vector2 DeltaPosition;
        
        public Vector2 StartPosition;
        
        public Vector2 EndPosition;
    }
}