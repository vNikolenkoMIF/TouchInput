using UnityEngine;

namespace TouchInput.Source.Gestures.Scripts.Modifiers
{
    /// <summary>
    /// Mouse simulation for the editor. Right mouse button acts as a second touch that starts
    /// as opposite mouse position. Recommends to use as touch emulation for UI events. 
    /// </summary>
    public class SimulateSecondTouchAsOppositePosition : IDragPositionModifier
    {
        private int _primaryId = int.MinValue;
        private int _secondaryId = int.MinValue;

        public Vector2 GetBeginPosition(int pointerId, Vector2 position)
        {
            if (_primaryId == int.MinValue) {
                _primaryId = pointerId;
                return position;
            }

            if (_secondaryId == int.MinValue) {
                _secondaryId = pointerId;
                return -position;
            }

            return position;
        }

        public Vector2 GetDragPosition(int pointerId, Vector2 position)
        {
            // Only the secondary touch mirrors position; primary follows the cursor exactly.
            if (pointerId == _secondaryId)
                return -position;
            return position;
        }

        public void EndDrag(int pointerId, Vector2 position)
        {
            if (pointerId == _primaryId) {
                _primaryId = int.MinValue;
            }
            else if (pointerId == _secondaryId) {
                _secondaryId = int.MinValue;
            }
        }
    }
}
