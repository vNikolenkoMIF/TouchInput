using UnityEngine;

namespace TouchInput.Source.Actions.Scripts.Contracts
{
    public struct TwoTouchesResult
    {
        public int InputId;
        
        public bool IsContactValid;
        
        public Vector2 FirstTouchPosition;
        
        public Vector2 SecondTouchPosition;
    }
}