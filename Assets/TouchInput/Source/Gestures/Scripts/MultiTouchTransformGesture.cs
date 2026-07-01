using TouchInput.Source.Actions.Scripts.Contracts;
using TouchInput.Source.Gestures.Scripts.Modifiers;
using TouchInput.Source.Managers.Scripts;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;

namespace TouchInput.Source.Gestures.Scripts
{
    /// <summary>
    /// Input System action–backed gesture recognizer. Reads from the "Touch (Single)" and
    /// "Touch (Multi)" actions in the Mobile action map via <see cref="InputActionsManager"/>.
    ///
    /// Pointer IDs are fixed: 0 = primary finger, 1 = secondary finger.
    ///
    /// Single→multi handoff: when a second finger arrives, Touch(Single) cancels and Touch(Multi)
    /// starts. The primary pointer (0) survives seamlessly — OnSingleCanceled checks whether
    /// Touch(Multi) is now valid and, if so, skips EndPointer so the gesture continues.
    ///
    /// Does not require a Graphic component or EventSystem raycaster on this GameObject.
    /// </summary>
    public sealed class MultiTouchTransformGesture : MultiTouchTransformGestureBase
    {
        [Header("Actions")]
        [SerializeField] 
        private InputActionName _singleTouchActionName = new("Touch (Single)");
        
        [SerializeField] 
        private InputActionName _multiTouchActionName = new("Touch (Multi)");
        
        private const int PRIMARY_ID   = 0;
        private const int SECONDARY_ID = 1;

        private InputAction _singleTouchAction;
        private InputAction _multiTouchAction;
        
        private IDragPositionModifier _multiTouchModifier;

        protected override void Awake()
        {
            base.Awake();
            InputActionsManager.Instance.TryGetAction(_singleTouchActionName, out _singleTouchAction);
            Assert.IsNotNull(_singleTouchAction);
            
            InputActionsManager.Instance.TryGetAction(_multiTouchActionName,  out _multiTouchAction);
            Assert.IsNotNull(_multiTouchAction);

            _multiTouchModifier = SystemInfo.deviceType == DeviceType.Handheld 
                ? new DragScreenPosition() 
                : new SimulateSecondTouchAsPivotOffset();
        }

        private void OnEnable()
        {
            if (_singleTouchAction != null) {
                _singleTouchAction.started   += OnSingleStarted;
                _singleTouchAction.performed += OnSinglePerformed;
                _singleTouchAction.canceled  += OnSingleCanceled;
            }

            if (_multiTouchAction != null) {
                _multiTouchAction.started   += OnMultiStarted;
                _multiTouchAction.performed += OnMultiPerformed;
                _multiTouchAction.canceled  += OnMultiCanceled;
            }
        }

        private void OnDisable()
        {
            if (_singleTouchAction != null) {
                _singleTouchAction.started   -= OnSingleStarted;
                _singleTouchAction.performed -= OnSinglePerformed;
                _singleTouchAction.canceled  -= OnSingleCanceled;
            }

            if (_multiTouchAction != null) {
                _multiTouchAction.started   -= OnMultiStarted;
                _multiTouchAction.performed -= OnMultiPerformed;
                _multiTouchAction.canceled  -= OnMultiCanceled;
            }
        }

        #region Single touch handlers
        private void OnSingleStarted(InputAction.CallbackContext ctx)
        {
            var v = ctx.ReadValue<TouchResult>();
            
            if (!v.IsContactValid) {
                return;
            }
            
            BeginPointer(PRIMARY_ID, v.Position);
        }

        private void OnSinglePerformed(InputAction.CallbackContext ctx)
        {
            var v = ctx.ReadValue<TouchResult>();
            
            if (!v.IsContactValid) {
                return;
            }
            
            MovePointer(PRIMARY_ID, v.Position);
        }

        private void OnSingleCanceled(InputAction.CallbackContext ctx)
        {
            if (_multiTouchAction.ReadValue<TwoTouchesResult>().IsContactValid) {
                return;
            }

            EndPointer(PRIMARY_ID);
        }
        #endregion Single touch


        #region Multi touch handlers
        private void OnMultiStarted(InputAction.CallbackContext ctx)
        {
            var v = ctx.ReadValue<TwoTouchesResult>();

            if (!v.IsContactValid){
                return;
            }

            var firstTouchPos = _multiTouchModifier.GetBeginPosition(PRIMARY_ID, v.FirstTouchPosition);
            var secondTouchPos = _multiTouchModifier.GetBeginPosition(SECONDARY_ID, v.SecondTouchPosition);
            
            // Primary may already be tracked by Touch(Single) — sync its position.
            BeginPointer(PRIMARY_ID, firstTouchPos);
            
            // Begin secondary finger. BeginPointer is a no-op if ID is already tracked.
            BeginPointer(SECONDARY_ID, secondTouchPos);
        }

        private void OnMultiPerformed(InputAction.CallbackContext ctx)
        {
            var v = ctx.ReadValue<TwoTouchesResult>();
            
            if (!v.IsContactValid){
                return;
            }

            var firstTouchPos = _multiTouchModifier.GetDragPosition(PRIMARY_ID, v.FirstTouchPosition);
            var secondTouchPos = _multiTouchModifier.GetDragPosition(SECONDARY_ID, v.SecondTouchPosition);
            
            MovePointer(PRIMARY_ID, firstTouchPos);
            MovePointer(SECONDARY_ID, secondTouchPos);
        }

        private void OnMultiCanceled(InputAction.CallbackContext ctx)
        {
            var twoTouchesResult = ctx.ReadValue<TwoTouchesResult>();
            
            EndPointer(SECONDARY_ID);
            _multiTouchModifier.EndDrag(SECONDARY_ID, twoTouchesResult.SecondTouchPosition);

            // End primary only if Touch(Single) is not still valid (primary still held).
            // If single-touch is valid, primary continues seamlessly without an End/Begin cycle.
            if (!_singleTouchAction.ReadValue<TouchResult>().IsContactValid) {
                _multiTouchModifier.EndDrag(PRIMARY_ID, twoTouchesResult.FirstTouchPosition);
                EndPointer(PRIMARY_ID);
            }

        }
        #endregion Multi touch handlers
    }
}
