using TouchInput.Source.Actions.Scripts.Contracts;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;

namespace TouchInput.Source.Actions.Scripts.Composites
{
    #if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
    #endif
    public class TwoFingersDragComposite : TwoFingersComposite<TwoTouchesResult>
    {
        [InputControl(layout = "Integer")]
        public int InputId;
        
        public override TwoTouchesResult ReadValue(ref InputBindingCompositeContext context)
        {
            return new TwoTouchesResult
            {
                InputId = context.ReadValue<int>(InputId),
                IsContactValid = IsTouchActive(context),
                FirstTouchPosition = GetTouchPosition(context),
                SecondTouchPosition = GetSecondaryTouchPosition(context)
            };
        }

        public override float EvaluateMagnitude(ref InputBindingCompositeContext context)
        {
            var isTouchActive = IsTouchActive(context);
            return isTouchActive ? 1f : 0f;

        }

#if UNITY_EDITOR
        static TwoFingersDragComposite()
        {
            Register();
        }

#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            InputSystem.RegisterBindingComposite<TwoFingersDragComposite>();
        }
    }
}