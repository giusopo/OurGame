using UnityEngine;

namespace OurGame.Core
{
    /// <summary>
    /// Simple MonoBehaviour singleton helper.
    /// Attach to any GameObject that should persist and be globally accessible.
    /// </summary>
    public class SingletonMono<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindExistingInstance();
                    if (_instance == null)
                    {
                        if (!Application.isPlaying)
                            return null;

                        GameObject go = new GameObject(typeof(T).Name);
                        _instance = go.AddComponent<T>();
                    }
                }

                EnsureRecoveredInstanceIsEnabled(_instance);
                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                if (Application.isPlaying)
                    DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                if (Application.isPlaying)
                    Destroy(gameObject);
                else
                    DestroyImmediate(gameObject);
            }
        }

        public static bool TryGetInstance(out T instance)
        {
            instance = _instance != null ? _instance : FindExistingInstance();
            EnsureRecoveredInstanceIsEnabled(instance);
            return instance != null;
        }

        private static T FindExistingInstance()
        {
            var activeObjects = FindObjectsByType<T>(FindObjectsSortMode.None);
            if (activeObjects.Length > 0)
                return activeObjects[0];

            T[] allObjects = Resources.FindObjectsOfTypeAll<T>();
            for (int i = 0; i < allObjects.Length; i++)
            {
                T candidate = allObjects[i];
                if (candidate == null)
                    continue;

                if (candidate is Component component && component.gameObject.scene.IsValid())
                    return candidate;
            }

            return null;
        }

        private static void EnsureRecoveredInstanceIsEnabled(T instance)
        {
            if (instance is Behaviour behaviour && !behaviour.enabled)
            {
                Debug.LogWarning(
                    $"{typeof(T).Name} was found disabled in the scene. Re-enabling it automatically."
                );
                behaviour.enabled = true;
            }
        }
    }
}
