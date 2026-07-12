//TODO: Add logging system

// Uncomment it when need to show debug logs  
//#define USE_DEBUG_LOGS

using System;
using System.Collections.Generic;
using TouchInput.Source.Gestures.Scripts.Contracts;
using TouchInput.Source.Managers.Scripts;
using TouchInput.Source.Utilities.Scripts;
using TouchInput.Source.Utilities.Scripts.Extensions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace TouchInput.Source.Gestures.Scripts
{
    /// <summary>
    /// Contains all gesture-recognition logic for single- and two-finger transform gestures
    /// (translation, rotation, scaling). Subclasses feed pointer events via
    /// <see cref="BeginPointer"/>, <see cref="MovePointer"/>, and <see cref="EndPointer"/>.
    /// </summary>
        public abstract class MultiTouchTransformGestureBase : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Minimum movement in cm before a gesture is recognized.")]
        private float _screenTransformThreshold = 0.1f;

        [SerializeField]
        [Tooltip("Minimum distance between two fingers in cm for multi-touch to be processed.")]
        private float _minFingersDistance = 0.4f;

        [SerializeField]
        [Tooltip("Which transforms are driven by single-finger input. Rotation uses horizontal drag as angle delta.")]
        private TransformType _singleTouchBindings = TransformType.Translation;

        [SerializeField]
        [Tooltip("Which transforms are driven by two-finger input. Translation uses finger midpoint delta.")]
        private TransformType _multiTouchBindings = TransformType.Rotation | TransformType.Scaling;

        [FormerlySerializedAs("_transformsLock")]
        [SerializeField]
        [Tooltip("Locked transforms are suppressed regardless of input source.")]
        private TransformType _lockedTransforms = TransformType.Nothing;

        [Header("Debug")]
        [SerializeField]
        private Vector2 _touchesDeltaPosition;
        
        [SerializeField]
        private float _deltaRotation;
        
        [SerializeField]
        private float _deltaScale;

        /// <summary>Delta dragging distance for active touch(es) in screen coordinates.</summary>
        public Vector2 DeltaPosition => _touchesDeltaPosition;

        /// <summary>Rotation delta in degrees. Positive = counter-clockwise.</summary>
        public float DeltaRotation => _deltaRotation;

        /// <summary>Fractional change in finger distance. Negative = fingers moved apart (zoom in), positive = fingers moved together (zoom out), 0 = no change.</summary>
        public float DeltaScale => _deltaScale;

        public TransformType SingleTouchBindings => _singleTouchBindings;
        
        public TransformType MultiTouchBindings => _multiTouchBindings;
        
        public TransformType LockedTransforms => _lockedTransforms;
        
        public event Action<InputActionPhase> TranslationPhaseChanged;
        public event Action<InputActionPhase> RotationPhaseChanged;
        public event Action<InputActionPhase> ScalePhaseChanged;

        /// <summary>Screen pixels per centimeter, resolved from <see cref="InputActionsManager"/>.</summary>
        protected float DotsPerCentimeter { get; private set; }

        private float _screenThresholdPixelsSquared;
        private float _minFingerDistancePixelsSquared;


        private bool _isTransforming;
        private bool _isScaling;
        private bool _isRotating;

        private Vector2 _translationBuffer;
        private float _translationBufferSquared;

        private float _rotationPixelBuffer;
        private float _rotationBuffer;

        private float _scalePixelBuffer;
        private float _scaleBuffer;

        private readonly Dictionary<int, Vector2> _currentPositions = new();
        private readonly Dictionary<int, Vector2> _previousPositions = new();

        #region Unity pipeline

        private void OnValidate() => ValidateBindings();

        protected virtual void Awake()
        {
            DotsPerCentimeter = InputActionsManager.Instance.DotsPerCentimeter;

            var thresholdPixels = _screenTransformThreshold * DotsPerCentimeter;
            _screenThresholdPixelsSquared = thresholdPixels * thresholdPixels;

            var minFingerPixels = _minFingersDistance * DotsPerCentimeter;
            _minFingerDistancePixelsSquared = minFingerPixels * minFingerPixels;
        }

        #endregion Unity pipeline

        #region TransformType management API

        /// <summary>Reassign which single-finger transform is active. Only one flag is allowed.</summary>
        public void RebindSingleTouchTransform(TransformType type)
        {
            if (type.MoreThanOneFlag())
                throw new InvalidOperationException("Cannot set more than one transform type to single-touch.");
            _singleTouchBindings = type;
            ValidateBindings();
        }

        public void RebindMultiTouchTransform(TransformType type)
        {
            _multiTouchBindings = type;
            ValidateBindings();
        }

        /// <summary>Lock chosen transformation(s) so they are suppressed regardless of input.</summary>
        public void SetTransformsLock(TransformType type) => _lockedTransforms = type;

        #endregion

        #region Pointer input — called by subclasses

        protected void BeginPointer(int pointerId, Vector2 position)
        {
            if (_currentPositions.ContainsKey(pointerId)) return;
#if USE_DEBUG_LOGS
            Debug.Log($"Dragging started by pointer: {pointerId}");
#endif

            _currentPositions[pointerId] = position;
            _previousPositions[pointerId] = position;

            if (IsAllLocked()) return;

            switch (_currentPositions.Count)
            {
                case 1:
                    StartSingleTouchEvents();
                    break;
                case 2:
                    ResetValues();
                    CancelSingleTouchEvents();
                    StartMultiTouchEvents();
                    break;
            }
        }

        protected void MovePointer(int pointerId, Vector2 position)
        {
            if (!_currentPositions.ContainsKey(pointerId)) return;

#if USE_DEBUG_LOGS
            Debug.Log($"Pointer: {pointerId} dragged to {position}");
#endif
            var delta = position - _currentPositions[pointerId];
            _previousPositions[pointerId] = _currentPositions[pointerId];
            _currentPositions[pointerId] = position;

            if (IsAllLocked()) return;

            switch (_currentPositions.Count)
            {
                case 1:
                    ProcessSingleTouch(delta);
                    break;
                case >= 2:
                    ProcessMultiTouch();
                    break;
            }
        }

        protected void EndPointer(int pointerId)
        {
#if USE_DEBUG_LOGS
            Debug.Log($"Dragging ended by pointer: {pointerId}");
#endif
            var countBefore = _currentPositions.Count;

            _currentPositions.Remove(pointerId);
            _previousPositions.Remove(pointerId);

            if (IsAllLocked()) return;

            if (countBefore >= 2 && _currentPositions.Count == 1)
            {
                CancelMultiTouchEvents();
                ResetValues();
                StartSingleTouchEvents();
            }
            else if (_currentPositions.Count == 0)
            {
                _isTransforming = false;
                _isRotating = false;
                _isScaling = false;

                if (countBefore == 1) {
                    CancelSingleTouchEvents();
                }
                else {
                    CancelMultiTouchEvents();
                }
            }
        }

        #endregion

        #region Gesture processing

        #region Single touch

        private void ProcessSingleTouch(Vector2 delta)
        {
            DoSingleTouchTranslation(delta);
            DoSingleTouchRotation(delta);
            DoSingleTouchScaling(delta);
        }

        private void DoSingleTouchTranslation(Vector2 delta)
        {
            if (!IsSingleTouchActive(TransformType.Translation)) return;

            if (!_isTransforming)
            {
                _translationBuffer += delta;
                if (!(_translationBuffer.sqrMagnitude >= _screenThresholdPixelsSquared)) return;

                _isTransforming = true;
                _touchesDeltaPosition = _translationBuffer;
            }
            else
            {
                _touchesDeltaPosition = delta;
            }

            TranslationPhaseChanged?.Invoke(InputActionPhase.Performed);
        }

        private void DoSingleTouchRotation(Vector2 delta)
        {
            if (!IsSingleTouchActive(TransformType.Rotation)) return;

            if (!_isRotating) {
                _rotationPixelBuffer += Mathf.Abs(delta.x);
                _rotationBuffer += delta.x;

                if (!(_rotationPixelBuffer * _rotationPixelBuffer >= _screenThresholdPixelsSquared)) {
                    return;
                }

                _isRotating = true;
                _deltaRotation = _rotationBuffer;
            }
            else {
                _deltaRotation = delta.x;
            }

            RotationPhaseChanged?.Invoke(InputActionPhase.Performed);
        }

        private void DoSingleTouchScaling(Vector2 delta)
        {
            if (!IsSingleTouchActive(TransformType.Scaling)) {
                return;
            }

            if (!_isScaling)
            {
                _scalePixelBuffer += Mathf.Abs(delta.y);
                _scaleBuffer *= delta.magnitude;

                if (!(_scalePixelBuffer * _scalePixelBuffer >= _screenThresholdPixelsSquared)) {
                    return;
                }

                _isScaling = true;
                _deltaScale = _scaleBuffer;
            }
            else
            {
                _deltaScale = delta.magnitude;
            }

            ScalePhaseChanged?.Invoke(InputActionPhase.Performed);
        }

        #endregion Single touch

        #region Multi touch

        private void ProcessMultiTouch()
        {
            using var it = _currentPositions.GetEnumerator();

            it.MoveNext();
            var keyA = it.Current.Key;
            var valA = it.Current.Value;

            it.MoveNext();
            var keyB = it.Current.Key;
            var valB = it.Current.Value;

            // Sort by id so SignedAngle is deterministic — swapping the two vectors reverses sign.
            var id0 = keyA < keyB ? keyA : keyB;
            var id1 = keyA < keyB ? keyB : keyA;
            var curr0 = keyA < keyB ? valA : valB;
            var curr1 = keyA < keyB ? valB : valA;

            var prev0 = _previousPositions.GetValueOrDefault(id0, curr0);
            var prev1 = _previousPositions.GetValueOrDefault(id1, curr1);

            DoMultiTouchRotationAndScaling(curr0, curr1, prev0, prev1);
            DoMultiTouchTranslation(curr1, prev0, prev1, _deltaRotation, _deltaScale);
        }

        private void DoMultiTouchTranslation(Vector2 curr1, Vector2 prev0, Vector2 prev1,
            float deltaRotation, float deltaScale)
        {
            if (!IsMultiTouchActive(TransformType.Translation)) {
                return;
            }

            if (_isTransforming)
            {
                var transformedPoint = ScaleAndRotate(prev0, (prev0 + prev1) * .5f, deltaRotation, deltaScale);
                _touchesDeltaPosition = transformedPoint;
                TranslationPhaseChanged?.Invoke(InputActionPhase.Performed);
            }

            _translationBuffer += curr1 - prev0;

            if (_translationBuffer.sqrMagnitude > _screenThresholdPixelsSquared)
            {
                _isTransforming = true;
                prev0 = curr1 - _translationBuffer;
                var transformedPoint = ScaleAndRotate(prev0, (prev0 + prev1) * .5f, deltaRotation, deltaScale);
                _touchesDeltaPosition = new Vector3(curr1.x - transformedPoint.x, curr1.y - transformedPoint.y, 0);
                TranslationPhaseChanged?.Invoke(InputActionPhase.Performed);
            }
        }

        private void DoMultiTouchRotationAndScaling(Vector2 curr0, Vector2 curr1, Vector2 prev0, Vector2 prev1)
        {
            var currVec = curr1 - curr0;
            var prevVec = prev1 - prev0;

            var prevMagnitude = prevVec.magnitude;
            var currMagnitude = currVec.magnitude;

            if (currVec.sqrMagnitude < _minFingerDistancePixelsSquared || prevMagnitude < 0.01f) {
                return;
            }

            var rotationEnabled = IsMultiTouchActive(TransformType.Rotation);
            var scalingEnabled = IsMultiTouchActive(TransformType.Scaling);

            if (rotationEnabled)
            {
                var frameDeltaAngle = Vector2.SignedAngle(prevVec, currVec);
                
                //Prevent axes flip when touches emulating by mouse 
                if (Mathf.Abs(frameDeltaAngle) > 90f) {
                    frameDeltaAngle = 0f;
                }
                
                if (_isRotating) {
                    _deltaRotation = frameDeltaAngle;
                    RotationPhaseChanged?.Invoke(InputActionPhase.Performed);
                }
                else {

                    // Perpendicular displacement of each finger relative to the previous baseline
                    // (the line through prev0/prev1) isolates the rotational component of the
                    // motion from the scaling component (which moves fingers along that line),
                    // in the same raw pixel units as the scaling threshold below. This keeps
                    // rotation equally responsive regardless of how far apart the fingers are —
                    // unlike an arc-length (radius * angle) measure, which under-detects rotation
                    // when fingers are close together.
                    PointToLineDistances(prev0, prev1, curr0, curr1, out var d0, out var d1);

                    _rotationPixelBuffer += d1 - d0;
                    _rotationBuffer += frameDeltaAngle;

                    if (_rotationPixelBuffer * _rotationPixelBuffer >= _screenThresholdPixelsSquared) {

                        _isRotating = true;
                        _deltaRotation = _rotationBuffer;
                        RotationPhaseChanged?.Invoke(InputActionPhase.Performed);
                    }
                }
            }

            if (scalingEnabled)
            {
                // Fractional change in finger distance — small and zero-centered, like
                // DeltaRotation (degrees/frame) and DeltaPosition (pixels/frame), both of which
                // are added directly to the target by consumers. The previous raw pixel
                // difference was unnormalized (tens/hundreds of units), which is why scaling
                // alone needed an outlier sensitivity (~0.001) to avoid blowing up the scale.
                var frameDeltaScale = (prevMagnitude - currMagnitude) / prevMagnitude;

                if (_isScaling) {
                    _deltaScale = frameDeltaScale;
                    ScalePhaseChanged?.Invoke(InputActionPhase.Performed);
                }
                else
                {
                    var scaleInPixels = currMagnitude - prevMagnitude;
                    
                    _scalePixelBuffer += scaleInPixels;
                    _scaleBuffer += frameDeltaScale;

                    if (_scalePixelBuffer * _scalePixelBuffer >= _screenThresholdPixelsSquared) {
                        
                        _isScaling = true;
                        _deltaScale = _scaleBuffer;
                        ScalePhaseChanged?.Invoke(InputActionPhase.Performed);
                    }
                }
            }
        }

        #endregion Multi touch

        #endregion Gesture processing

        #region Gesture lifecycle helpers

        private void ResetValues()
        {
            _isTransforming = false;
            _translationBuffer = Vector2.zero;
            
            _isRotating = false;
            _rotationPixelBuffer = 0f;
            _rotationBuffer = 0f;
            _deltaRotation = 0f;
            
            _isScaling = false;
            _scalePixelBuffer = 0f;
            _scaleBuffer = 0f;
            _deltaScale = 0f;
        }

        private void StartSingleTouchEvents()
        {
            if (IsSingleTouchActive(TransformType.Translation)) 
                TranslationPhaseChanged?.Invoke(InputActionPhase.Started);
            
            if (IsSingleTouchActive(TransformType.Rotation))  
                RotationPhaseChanged?.Invoke(InputActionPhase.Started);
            
            if (IsSingleTouchActive(TransformType.Scaling))  
                ScalePhaseChanged?.Invoke(InputActionPhase.Started);
        }

        private void StartMultiTouchEvents()
        {
            if (IsMultiTouchActive(TransformType.Translation)) 
                TranslationPhaseChanged?.Invoke(InputActionPhase.Started);
            
            if (IsMultiTouchActive(TransformType.Rotation))    
                RotationPhaseChanged?.Invoke(InputActionPhase.Started);
            
            if (IsMultiTouchActive(TransformType.Scaling))     
                ScalePhaseChanged?.Invoke(InputActionPhase.Started);
        }

        private void CancelSingleTouchEvents()
        {
            if (IsSingleTouchActive(TransformType.Translation)){ 
                _touchesDeltaPosition = Vector2.zero; 
                TranslationPhaseChanged?.Invoke(InputActionPhase.Canceled); 
            }

            if (IsSingleTouchActive(TransformType.Rotation)) {
                _deltaRotation = 0f;                  
                RotationPhaseChanged?.Invoke(InputActionPhase.Canceled);
            }

            if (IsSingleTouchActive(TransformType.Scaling)) {
                _deltaScale = 0f;
                ScalePhaseChanged?.Invoke(InputActionPhase.Canceled);
            }
        }

        private void CancelMultiTouchEvents()
        {
            if (IsMultiTouchActive(TransformType.Translation)) {
                _touchesDeltaPosition = Vector2.zero; 
                TranslationPhaseChanged?.Invoke(InputActionPhase.Canceled);
            }
            
            if (IsMultiTouchActive(TransformType.Rotation)) { 
                _deltaRotation = 0f;                  
                RotationPhaseChanged?.Invoke(InputActionPhase.Canceled); 
            }

            if (IsMultiTouchActive(TransformType.Scaling)) {
                _deltaScale = 0f;
                ScalePhaseChanged?.Invoke(InputActionPhase.Canceled);
            }
        }

        #endregion Gesture lifecycle helpers

        #region Bindings validation

        private bool IsSingleTouchActive(TransformType type) =>
            _singleTouchBindings.HasFlag(type) && !_lockedTransforms.HasFlag(type);

        private bool IsMultiTouchActive(TransformType type) =>
            _multiTouchBindings.HasFlag(type) && !_lockedTransforms.HasFlag(type);

        private bool IsAllLocked() =>
            _lockedTransforms.HasFlag(TransformType.Everything);

        private bool ValidateBindings()
        {
            if (_singleTouchBindings.HasFlag(TransformType.Everything) || _singleTouchBindings.MoreThanOneFlag())
            {
                _singleTouchBindings = TransformType.Translation;
                _multiTouchBindings |= TransformType.Everything & ~_singleTouchBindings;
                return false;
            }

            if (_multiTouchBindings.HasFlag(_singleTouchBindings))
                _multiTouchBindings &= ~_singleTouchBindings;
            return true;
        }

        #endregion Bindings validation

        #region Math helpers

        private static Vector2 ScaleAndRotate(Vector2 point, Vector2 center, float dR, float dS)
        {
            var delta = point - center;
            if (dR != 0) delta = Rotate(delta, dR);
            if (dS != 0) delta *= dS;
            return center + delta;
        }

        private static Vector2 Rotate(Vector2 point, float angle)
        {
            var rad = angle * Mathf.Deg2Rad;
            var cos = Mathf.Cos(rad);
            var sin = Mathf.Sin(rad);
            return new Vector2(point.x * cos - point.y * sin, point.x * sin + point.y * cos);
        }

        /// <summary>Signed perpendicular distance of <paramref name="point0"/>/<paramref name="point1"/> from the infinite line through <paramref name="lineStart"/>/<paramref name="lineEnd"/>.</summary>
        private static void PointToLineDistances(Vector2 lineStart, Vector2 lineEnd, Vector2 point0, Vector2 point1, out float d0, out float d1)
        {
            var lineDir = (lineEnd - lineStart).normalized;
            var normal = new Vector2(-lineDir.y, lineDir.x);
            d0 = Vector2.Dot(point0 - lineStart, normal);
            d1 = Vector2.Dot(point1 - lineStart, normal);
        }

        #endregion Math helpers
    }
}
