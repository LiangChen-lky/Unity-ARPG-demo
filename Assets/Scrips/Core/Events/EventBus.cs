using System;
using System.Collections.Generic;

public class EventBus : Singleton<EventBus>
{
    private static readonly Dictionary<Type, List<object>> handlers = new();
    
    public static void Subscribe<T>(Action<T> handler) where T : IEvent
    {
        var type = typeof(T);
        if (!handlers.TryGetValue(type, out var list))
        {
            list = new List<object>();
            handlers[type] = list;
        }
        list.Add(handler);
    }

    public static void Unsubscribe<T>(Action<T> handler) where T : IEvent
    {
        var type = typeof(T);
        if (handlers.TryGetValue(type, out var list))
        {
            list.Remove(handler);
        }
    }

    public static void Publish<T>(T eventArgs) where T : IEvent
    {
        var type = typeof(T);
        if (handlers.TryGetValue(type, out var list))
        {
            foreach (var handler in list)
            {
                ((Action<T>)handler)?.Invoke(eventArgs);
            }
        }
    }
    
}
