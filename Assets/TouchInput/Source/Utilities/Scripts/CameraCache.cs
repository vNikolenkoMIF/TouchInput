using UnityEngine;

namespace TouchInput.Source.Utilities.Scripts
{
    public class CameraCache
    {
        private static Camera _cachedCamera;

        /// <summary>
        /// Returns a cached reference to the main camera and uses Camera.main if it hasn't been cached yet.
        /// </summary>
        // ReSharper disable once InconsistentNaming
#pragma warning disable IDE1006
        public static Camera main
#pragma warning restore IDE1006
        {
            get
            {
                if (_cachedCamera == null) {
                    return Refresh(Camera.main);
                }
                return _cachedCamera;
            }
        }

        /// <summary>
        /// Set the cached camera to a new reference and return it
        /// </summary>
        /// <param name="newCamera">New main camera to store</param>
        public static Camera Refresh(Camera newCamera)
        {
            return _cachedCamera = newCamera;
        }
    }
}
