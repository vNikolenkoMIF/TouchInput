using UnityEngine;

namespace TouchInput.Source.Utilities.Scripts.Extensions
{
    public static class GameObjectExtensions
    {
        public static GameObject GetParentRoot(this GameObject child)
        {
            return child.transform.parent == null ? child : GetParentRoot(child.transform.parent.gameObject);
        }
    }
}
