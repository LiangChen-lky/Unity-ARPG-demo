using System;
using System.Collections.Generic;

public class EventBus : Singleton<EventBus>
{
    private static readonly Dictionary<Type, List<object>> handlers = new();

    private EventBus() : base()
    {
    }

    public static void Subscribe<T>(Action<T> handler) where T : IEvent
    {
        if (handler == null) return;

        var type = typeof(T);
        if (!handlers.TryGetValue(type, out var list))
        {
            list = new List<object>();
            handlers[type] = list;
        }

        if (!list.Contains(handler))
            list.Add(handler);
    }

    public static void Unsubscribe<T>(Action<T> handler) where T : IEvent
    {
        if (handler == null) return;

        var type = typeof(T);
        if (!handlers.TryGetValue(type, out var list))
            return;

        if (!list.Remove(handler))
            return;

        if (list.Count == 0)
            handlers.Remove(type);
    }

    public static void Publish<T>(T eventArgs) where T : IEvent
    {
        var type = typeof(T);
        if (!handlers.TryGetValue(type, out var list) || list.Count == 0)
            return;

        var snapshot = new object[list.Count];
        list.CopyTo(snapshot);

        foreach (var handler in snapshot)
            ((Action<T>)handler)?.Invoke(eventArgs);
    }
}
