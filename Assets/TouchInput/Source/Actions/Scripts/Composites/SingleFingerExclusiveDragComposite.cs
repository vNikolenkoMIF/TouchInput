using TouchInput.Source.Actions.Scripts.Contracts;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;

namespace TouchInput.Source.Actions.Scripts.Composites
{
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    public class SingleFingerExclusiveDragComposite : OneFingerComposite<TouchFeedback>
    {
        [InputControl(layout = "Integer")]
        public int InputId;

        [InputControl(layout = "Button")]
        public int SecondaryTouch;

        protected override bool IsTouchActive(InputBindingCompositeContext ctx) =>
            ctx.ReadValueAsButton(PrimaryTouch) && !ctx.ReadValueAsButton(SecondaryTouch);

        public override TouchFeedback ReadValue(ref InputBindingCompositeContext context)
        {
            return new TouchFeedback
            {
                InputId = context.ReadValue<int>(InputId),
                IsContactValid = IsTouchActive(context),
                Position = GetTouchPosition(context),
            };
        }

        public override float EvaluateMagnitude(ref InputBindingCompositeContext context)
            => IsTouchActive(context) ? 1f : 0f;

#if UNITY_EDITOR
        static SingleFingerExclusiveDragComposite()
        {
            Register();
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            InputSystem.RegisterBindingComposite<SingleFingerExclusiveDragComposite>();
        }
    }
}
