using UnityEngine;

namespace TouchInput.Source.Utilities.Scripts
{
    public static class VectorExtensions
    {
        public static Vector3 Clamp(this Vector3 vector, float min, float max)
        {
            vector.x = Mathf.Clamp(vector.x, min, max);
            vector.y = Mathf.Clamp(vector.y, min, max);
            vector.z = Mathf.Clamp(vector.z, min, max);
            return vector;
        }
    }
}
