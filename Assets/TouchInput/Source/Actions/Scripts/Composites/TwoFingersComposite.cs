using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;

namespace TouchInput.Source.Actions.Scripts.Composites
{
    public abstract class TwoFingersComposite<T> : OneFingerComposite<T> where T : struct
    {
        [InputControl(layout = "Button")]
        public int SecondaryTouch;
        
        [InputControl(layout = "Vector2")]
        public int SecondaryPosition;

        protected override bool IsTouchActive(InputBindingCompositeContext ctx) =>
            ctx.ReadValueAsButton(PrimaryTouch) && ctx.ReadValueAsButton(SecondaryTouch);

        protected Vector2 GetSecondaryTouchPosition(InputBindingCompositeContext ctx)
            => ctx.ReadValue<Vector2, Vector2MagnitudeComparer>(SecondaryPosition);
    }
}