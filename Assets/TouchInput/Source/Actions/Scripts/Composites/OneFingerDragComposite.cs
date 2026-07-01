using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;

namespace TouchInput.Source.Actions.Scripts.Composites
{
    #if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
    #endif
    public class OneFingerDragComposite : OneFingerComposite<Contracts.TouchFeedback>
    {
        [InputControl(layout = "Integer")]
        public int InputId;
        
        public override Contracts.TouchFeedback ReadValue(ref InputBindingCompositeContext context)
        {
            return new Contracts.TouchFeedback
            {
                InputId = context.ReadValue<int>(InputId),
                IsContactValid = IsTouchActive(context),
                Position = GetTouchPosition(context),
            };
        }

        public override float EvaluateMagnitude(ref InputBindingCompositeContext context)
            => IsTouchActive(context) ? 1f : 0f;
        
#if UNITY_EDITOR
        static OneFingerDragComposite()
        {
            Register();
        }

#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            InputSystem.RegisterBindingComposite<OneFingerDragComposite>();
        }
    }
}
