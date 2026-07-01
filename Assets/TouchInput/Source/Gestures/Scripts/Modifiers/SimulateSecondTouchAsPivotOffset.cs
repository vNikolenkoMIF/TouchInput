using UnityEngine;

namespace TouchInput.Source.Gestures.Scripts.Modifiers
{
    /// <summary>
    /// Mouse simulation for the editor. Right mouse button acts as a second touch that starts
    /// with the pivot (midpoint) with small offset from the mouse position.
    /// Recommends to use for Input Actions emulation.
    /// </summary>
    public class SimulateSecondTouchAsPivotOffset : IDragPositionModifier
    {
        private int _primaryId = int.MinValue;
        private int _secondaryId = int.MinValue;
        
        // Stores the screen coordinate anchor around which the secondary touch will mirror.
        private Vector2 _pivotPosition;

        public Vector2 GetBeginPosition(int pointerId, Vector2 screenPosition)
        {
            if (_primaryId == int.MinValue) {
                _primaryId = pointerId;
                return screenPosition;
            }

            if (_secondaryId == int.MinValue) {
                _secondaryId = pointerId;
                
                // Establish the pivot at the current cursor position the moment the second touch is registered.
                _pivotPosition = screenPosition;
                
                // Introduce a small default offset to prevent a distance of zero pixels at initialization.
                // This prevents accidental division-by-zero errors in distance/scale tracking systems.
                var initialOffset = new Vector2(100f, 0f); 
                return _pivotPosition - initialOffset;
            }

            return screenPosition;
        }

        public Vector2 GetDragPosition(int pointerId, Vector2 screenPosition)
        {
            // The primary finger (left mouse click) mirrors standard cursor screen movement exactly.
            if (pointerId == _primaryId)
                return screenPosition;

            // The secondary finger mirrors symmetrically relative to the locked pivot point.
            if (pointerId == _secondaryId)
            {
                // Calculate directional delta from the pivot position to the active cursor path.
                var offsetFromPivot = screenPosition - _pivotPosition;
                
                // Projects the secondary pointer identically along the inverted trajectory path.
                return _pivotPosition - offsetFromPivot;
            }
                
            return screenPosition;
        }

        public void EndDrag(int pointerId, Vector2 screenPosition)
        {
            if (pointerId == _primaryId) {
                _primaryId = int.MinValue;
            }
            else if (pointerId == _secondaryId) {
                _secondaryId = int.MinValue;
                _pivotPosition = Vector2.zero;
            }
        }
    }
}
