using UnityEngine;

namespace TouchInput.Source.Actions.Scripts.Contracts
{
    public struct TouchFeedback
    {
        public bool IsContactValid;

        /// <summary>
        /// ID of input type.
        /// </summary>
        public int InputId;

        /// <summary>
        /// Position of draw input.
        /// </summary>
        public Vector2 Position;
    }
}
