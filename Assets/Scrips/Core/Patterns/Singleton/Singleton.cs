using System;

public abstract class Singleton<T> where T : class
{
    private static readonly object SyncRoot = new object();
    private static T instance;
    private static bool isCreating;

    protected Singleton()
    {
        if (!isCreating)
        {
            throw new InvalidOperationException(
                $"{typeof(T).Name} must be created via {nameof(Instance)}.");
        }
    }

    public static T Instance
    {
        get
        {
            if (instance != null) return instance;

            // 防止多线程环境下重复创建实例
            lock (SyncRoot)
            {
                if (instance != null) return instance;

                isCreating = true;
                try
                {
                    instance = (T)Activator.CreateInstance(typeof(T), nonPublic: true);
                }
                finally
                {
                    isCreating = false;
                }

                if (instance is ISingletonInitializable init)
                {
                    init.OnInitialize();
                }

                return instance;
            }
        }
    }

    public static bool IsCreated => instance != null;

    public static void DisposeInstance()
    {
        lock (SyncRoot)
        {
            if (instance is IDisposable disposable)
            {
                disposable.Dispose();
            }

            instance = null;
        }
    }

#if UNITY_INCLUDE_TESTS
    internal static void ResetForTests() => DisposeInstance();
#endif
}

public interface ISingletonInitializable
{
    void OnInitialize();
}