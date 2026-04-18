using UnityEngine;

/// <summary>
/// Base class for MonoBehaviour-based singletons that require Unity lifecycle methods.
/// Use this for managers that need to participate in Unity's update cycle,
/// such as audio players, coroutine managers, or other MonoBehaviour-dependent systems.
/// </summary>
/// <typeparam name="T">The derived class type</typeparam>
public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    private static T instance;
    private static bool applicationIsQuitting = false;
    private static readonly object @lock = new object();

    /// <summary>
    /// Gets the singleton instance. Creates one if it doesn't exist.
    /// </summary>
    public static T Instance
    {
        get
        {
            if (applicationIsQuitting)
            {
                Debug.LogWarning($"[MonoSingleton] Instance '{typeof(T)}' already destroyed. " +
                               "Returning null. This can happen when accessing singleton in OnDestroy.");
                return null;
            }

            lock (@lock)
            {
                if (instance == null)
                {
                    // Look for existing instance in the scene
                    instance = FindFirstObjectByType<T>();

                    if (instance == null)
                    {
                        // Create new GameObject with the singleton component
                        GameObject singletonObject = new GameObject($"{typeof(T).Name} (Singleton)");
                        instance = singletonObject.AddComponent<T>();

                        // Apply DontDestroyOnLoad if configured
                        if (instance.ShouldPersistAcrossScenes)
                        {
                            DontDestroyOnLoad(singletonObject);
                        }

                        Debug.Log($"[MonoSingleton] Created new instance of {typeof(T)}");
                    }
                    else
                    {
                        // Ensure persistence setting is applied to existing instance
                        if (instance.ShouldPersistAcrossScenes && instance.gameObject.scene.buildIndex != -1)
                        {
                            DontDestroyOnLoad(instance.gameObject);
                        }
                    }
                }

                return instance;
            }
        }
    }

    /// <summary>
    /// Check if singleton instance exists (created and not destroyed)
    /// </summary>
    public static bool HasInstance => instance != null && !applicationIsQuitting;

    /// <summary>
    /// Override this to control whether the singleton GameObject persists across scene loads.
    /// Default is true (persist across scenes).
    /// </summary>
    protected virtual bool ShouldPersistAcrossScenes => true;

    /// <summary>
    /// Called when the singleton instance is created and initialized.
    /// Override this for initialization logic that should run after Awake but before Start.
    /// </summary>
    protected virtual void OnSingletonCreated() { }

    /// <summary>
    /// Called when the singleton instance is being destroyed.
    /// Override this for cleanup logic.
    /// </summary>
    protected virtual void OnSingletonDestroy() { }

    #region Unity Lifecycle Methods

    protected virtual void Awake()
    {
        lock (@lock)
        {
            if (instance == null)
            {
                // First instance - set as singleton
                instance = this as T;

                // Apply persistence if needed
                if (ShouldPersistAcrossScenes && gameObject.scene.buildIndex != -1)
                {
                    DontDestroyOnLoad(gameObject);
                }

                // Call initialization hook
                OnSingletonCreated();
            }
            else if (instance != this)
            {
                // Duplicate instance found - destroy this one
                Debug.LogWarning($"[MonoSingleton] Duplicate instance of {typeof(T)} found. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }
        }
    }

    protected virtual void OnDestroy()
    {
        lock (@lock)
        {
            if (instance == this)
            {
                OnSingletonDestroy();
                instance = null;
            }
        }
    }

    protected virtual void OnApplicationQuit()
    {
        applicationIsQuitting = true;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Ensures the singleton instance exists (creates it if necessary).
    /// Useful for lazy initialization.
    /// </summary>
    public static void EnsureInstanceExists()
    {
        if (!HasInstance)
        {
            var _ = Instance; // Accessing Instance property will create it if needed
        }
    }

    /// <summary>
    /// Manually destroy the singleton instance.
    /// Use with caution - after calling this, Instance property will create a new instance when accessed.
    /// </summary>
    public static void DestroyInstance()
    {
        lock (@lock)
        {
            if (instance != null)
            {
                Destroy(instance.gameObject);
                instance = null;
            }
        }
    }

    #endregion
}
