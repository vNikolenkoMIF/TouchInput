using TouchInput.Source.Gestures.Scripts;
using TouchInput.Source.Transformers.Scripts.Contracts;
using TouchInput.Source.Utilities.Scripts;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TouchInput.Source.Transformers.Scripts
{
    [RequireComponent(typeof(MultiTouchTransformGesture))]
    public class ThreeDimensionalTransformation : MonoBehaviour
    {
        [SerializeField] 
        private MultiTouchTransformGesture _gesture;
        
        [Header("Translation properties")]
        [SerializeField]
        private TransformProperties _translationProperties = new();
        
        [Header("Rotation properties")]
        [SerializeField]
        private TransformProperties _rotationProperties = new();
        
        [Header("Scale properties")]
        [SerializeField]
        private TransformProperties _scaleProperties = new();
        
        [SerializeField]
        private float _minScale = 0.1f;

        [SerializeField]
        private float _maxScale = 10f;
        
  
        public Transform TranslateTransform
        {
            get => _translationProperties.Transform;
            set
            {
                if (!value) {
                    return;
                }
                
                _translationProperties.Transform = value;
                _targetPosition = value.position;
            }
        }

        public Transform RotationTransform
        {
            get => _rotationProperties.Transform;
            set
            {
                if (!value) {
                    return;
                }
                
                _rotationProperties.Transform = value;
                _targetRotation = value.rotation;
            }
        }

        public Transform ScaleTransform
        {
            get => _scaleProperties.Transform;
            set
            {
                if (!value) {
                    return;
                }
                
                _scaleProperties.Transform = value;
                _targetScale = value.localScale;
            }
        }
        
        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private Vector3 _targetScale;

#if UNITY_EDITOR
        private void OnValidate()
        {
            _gesture = GetComponent<MultiTouchTransformGesture>();
            
            if (!_translationProperties.Transform) {
                _translationProperties.Transform = transform;
            }
            
            if (!_rotationProperties.Transform) {
                _rotationProperties.Transform = transform;
            }

            if (!_scaleProperties.Transform)
            {
                _scaleProperties.Transform = transform;
            }
        }
#endif
        
        private void Awake()
        {
            if (_gesture == null) {
                _gesture = GetComponent<MultiTouchTransformGesture>();
            }
            
            if (!TranslateTransform) {
                TranslateTransform = transform;
            }
            else {
                _targetPosition = TranslateTransform.position;
            }

            if (!RotationTransform) {
                RotationTransform = transform;
            }
            else {
                _targetRotation = RotationTransform.rotation;
            }

            if (!ScaleTransform) {
                ScaleTransform = transform;
            }
            else {
                _targetScale =  ScaleTransform.localScale;
            }
        }

        private void OnEnable()
        {
            _gesture.TranslationPhaseChanged += HandleTranslationPhase;
            _gesture.RotationPhaseChanged += HandleRotationPhase;
            _gesture.ScalePhaseChanged += HandleScalePhase;
        }

        private void OnDisable()
        {
            _gesture.TranslationPhaseChanged -= HandleTranslationPhase;
            _gesture.RotationPhaseChanged -= HandleRotationPhase;
            _gesture.ScalePhaseChanged -= HandleScalePhase;
        }

        private void Update()
        {
            if (IsTransformAwayFromPosition(_targetPosition)) {
                RunSmoothTranslationStep(_targetPosition, _translationProperties.GetSmoothingFraction());
            }

            if (Quaternion.Angle(RotationTransform.localRotation, _targetRotation) > _rotationProperties.MinApproximatedDistance) {
                RunSmoothRotationStep(_targetRotation, _rotationProperties.GetSmoothingFraction());
            }
            else {
                RotationTransform.localRotation = _targetRotation;
            }
            

            ScaleTransform.localScale = (ScaleTransform.localScale - _targetScale).sqrMagnitude > _scaleProperties.MinApproximatedDistance 
                ? Vector3.Lerp(ScaleTransform.localScale, _targetScale, _scaleProperties.GetSmoothingFraction()) 
                : _targetScale;
        }

        private void HandleTranslationPhase(InputActionPhase phase)
        {
            switch (phase)
            {
                case InputActionPhase.Started:
                    _targetPosition = TranslateTransform.position;
                    break;
                case InputActionPhase.Performed:
                    _targetPosition += ScreenDeltaToWorld(_gesture.DeltaPosition) * _translationProperties.Sensitivity;
                    break;
                case InputActionPhase.Canceled:
                    break;
            }
        }
        
        private bool IsTransformAwayFromPosition(Vector3 position)
        {
            return Vector3.Distance(TranslateTransform.position, position) > _translationProperties.MinApproximatedDistance;
        }

        private Vector3 ScreenDeltaToWorld(Vector2 screenDelta)
        {
            var cam = CameraCache.main;
            var depth = cam.WorldToScreenPoint(TranslateTransform.position).z;
            var worldOrigin = cam.ScreenToWorldPoint(new Vector3(0f, 0f, depth));
            var worldDelta = cam.ScreenToWorldPoint(new Vector3(screenDelta.x, screenDelta.y, depth));
            return worldDelta - worldOrigin;
        }
        
        private void RunSmoothTranslationStep(Vector3 position, float fraction)
        {
            TranslateTransform.position = Vector3.Lerp(TranslateTransform.position, position, fraction);
        }

        private void HandleRotationPhase(InputActionPhase phase)
        {
            if (phase != InputActionPhase.Performed) {
                return;
            }
            
            _targetRotation = Quaternion.AngleAxis(_gesture.DeltaRotation * _rotationProperties.Sensitivity, 
                Vector3.forward) * _targetRotation;
        }
        
        private void RunSmoothRotationStep(Quaternion targetRotation, float fraction)
        {
            RotationTransform.localRotation = Quaternion.Slerp(RotationTransform.localRotation, targetRotation, fraction);
        }

        private void HandleScalePhase(InputActionPhase phase)
        {
            if (phase != InputActionPhase.Performed) {
                return;
            }

            var ratio = -_gesture.DeltaScale * _scaleProperties.Sensitivity;
            var delta = new Vector3(ratio, ratio, 0);
            
            _targetScale = (_targetScale + delta).Clamp(_minScale, _maxScale);
        }
    }
}