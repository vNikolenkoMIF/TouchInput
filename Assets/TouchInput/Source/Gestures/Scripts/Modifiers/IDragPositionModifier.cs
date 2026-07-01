using UnityEngine;

namespace TouchInput.Source.Gestures.Scripts.Modifiers
{
    public interface IDragPositionModifier
    {
        public Vector2 GetBeginPosition(int pointerId, Vector2 screenPosition);
        
        public Vector2 GetDragPosition(int pointerId, Vector2 screenPosition);
        
        public void EndDrag(int pointerId, Vector2 screenPosition);
    }
}