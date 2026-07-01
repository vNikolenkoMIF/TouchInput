using System;
using UnityEngine;

namespace TouchInput.Source.Transformers.Scripts.Contracts
{
    [Serializable]
    public class TransformProperties
    {
        [SerializeField]
        private Transform _transform;
        
        [Range(0, 1)]
        [SerializeField]
        [Tooltip("Transformation smoothing factor. 0 - instant (most responsive), 1 - slowest.")]
        private float _smoothingFactor = 0.1f;
        
        [SerializeField]
        [Range(0, 10)]
        [Tooltip("Minimum distance between current and target position before lerp stops. " +
                 "Prevents micro-jitter when the object is close to the target.")]
        private float _minApproximatedDistance = 0.2f;
        
        [SerializeField]
        [Range(0f, 100f)]
        [Tooltip("Transformation sensitivity factor")]
        private float _sensitivity = 1f;
        
        public float SmoothingFactor
        {
            get => _smoothingFactor;
            set => _smoothingFactor = Mathf.Clamp(value, 0f, 1f);
        }

        public float MinApproximatedDistance
        {
            get => _minApproximatedDistance;
            set => _minApproximatedDistance = Mathf.Clamp(value, 0f, 10f);
        }

        public Transform Transform
        {
            get => _transform;
            set => _transform = value;
        }

        public float Sensitivity
        {
            get => _sensitivity;
            set => _sensitivity = Mathf.Clamp(value, 0f, 100f);
        }

        public float GetSmoothingFraction()
        {
            return 1f - Mathf.Pow(_smoothingFactor, Time.unscaledDeltaTime);
        }
    }
}