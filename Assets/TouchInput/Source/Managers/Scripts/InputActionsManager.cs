using TouchInput.Source.Utilities.Scripts;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;

namespace TouchInput.Source.Managers.Scripts
{
    [DefaultExecutionOrder(int.MinValue)]
    public sealed class InputActionsManager : MonoSingleton<InputActionsManager>
    {
        [SerializeField] 
        private float _referenceDpi = 96; 
        
        [SerializeField]
        private InputActionAsset _inputActionAsset;

        public float DotsPerCentimeter => (float)MeasurementUtils.CmToInches(_referenceDpi); 
        
        protected override void Awake()
        {
            base.Awake();
            
            Assert.IsNotNull(_inputActionAsset);
        }

        public bool TryGetAction(string actionName, out InputAction inputAction)
        {
            if (string.IsNullOrEmpty(actionName)) {
                inputAction = null;
                return false;
            }

            inputAction = _inputActionAsset.FindAction(actionName);
            return inputAction != null;
        }
    }
}
