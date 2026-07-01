using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;

namespace TouchInput.Source.Actions.Scripts.Composites
{
    public abstract class OneFingerComposite<T> : InputBindingComposite<T> where T : struct
    {
        [InputControl(layout = "Button")]
        public int PrimaryTouch;

        [InputControl(layout = "Vector2")]
        public int PrimaryPosition;
        
        protected virtual bool IsTouchActive(InputBindingCompositeContext ctx) => ctx.ReadValueAsButton(PrimaryTouch);
        
        protected Vector2 GetTouchPosition(InputBindingCompositeContext ctx)
            => ctx.ReadValue<Vector2, Vector2MagnitudeComparer>(PrimaryPosition);
    }
}
