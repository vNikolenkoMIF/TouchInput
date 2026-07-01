using UnityEngine;

namespace TouchInput.Source.Utilities.Scripts
{
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
       private static T _instance = null;
        public static T Instance
        {
            get
            {
                // Instance required for the first time, we look for it
                if (_instance == null && _searchForInstance) {

                    _searchForInstance = false;
                    var foundInstances = FindObjectsByType<T>(FindObjectsSortMode.None);

                    if (foundInstances.Length == 0) {
                        Debug.LogError($"No objects of type {typeof(T).Name} have been added to the scene");
                        return _instance;
                    }

                    if (foundInstances.Length == 1) {
                        _instance = foundInstances[0];
                        SetDontDestroyOnLoad(_instance.gameObject);
                    }
                    else if (foundInstances.Length > 1) {
                        Debug.LogErrorFormat("Expected exactly 1 {0} but found {1}.", typeof(T).Name, foundInstances.Length);
                    }
                }

                return _instance;
            }
        }

        public bool IsInitialized => _instance != null;


        private static bool _searchForInstance = true;

        protected virtual void Awake()
        {
            if (IsInitialized && _instance != this)
            {
                if (Application.isEditor) {
                    DestroyImmediate(this);
                }
                else {
                    Destroy(this);
                }

                Debug.LogErrorFormat("Trying to instantiate a second instance of singleton class {0}. Additional Instance was destroyed", GetType().Name);
            }
            else if (!IsInitialized)
            {
                _instance = (T)this;
                SetDontDestroyOnLoad(_instance.gameObject);
                _searchForInstance = false;
            }
        }

        /// <summary>
        /// Base OnDestroy method that destroys the Singleton's unique instance.
        /// Called by Unity when destroying a MonoBehaviour. Scripts that extend
        /// Singleton should be sure to call base.OnDestroy() to ensure the
        /// underlying static Instance reference is properly cleaned up.
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (_instance != this) {
                return;
            }
            _instance = null;
            _searchForInstance = true;
        }

        private static void SetDontDestroyOnLoad(GameObject instance)
        {
            var parent = instance.GetParentRoot();

#if UNITY_EDITOR // Skip Don't Destroy On Load when editor isn't playing so test runner passes.
            if (UnityEditor.EditorApplication.isPlaying)
#endif
                DontDestroyOnLoad(parent);
        }
    }
}
