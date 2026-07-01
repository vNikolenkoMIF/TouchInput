using UnityEngine;

namespace TouchInput.Source.Gestures.Scripts.Modifiers
{
    /// <summary>
    /// Real touch screen position extractor
    /// </summary>
    public class DragScreenPosition : IDragPositionModifier
    {
        public Vector2 GetBeginPosition(int pointerId, Vector2 position)
        {
            return position;
        }

        public Vector2 GetDragPosition(int pointerId, Vector2 position)
        {
            return position;
        }

        public void EndDrag(int pointerId, Vector2 screenPosition)
        {
            
        }
    }
}