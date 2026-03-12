using System;
using System.Collections.Generic;
using UnityEngine;

public enum ModelOperationEventType
{
    Drag,
    // 可以添加更多事件类型
}

public static class ModelOperationEventManager
{
    private static readonly Dictionary<Type, List<Action<object>>> eventHandlers = new Dictionary<Type, List<Action<object>>>();
    //private static readonly Dictionary<Type, Dictionary<Tuple<object, MethodInfo>, List<Action<object>>>> eventHandlers =
    //    new Dictionary<Type, Dictionary<Tuple<object, MethodInfo>, List<Action<object>>>>();

    public static void Subscribe<T>(Action<T> handler) where T : class
    {
        Type eventType = typeof(T);
        if (!eventHandlers.ContainsKey(eventType))
        {
            eventHandlers[eventType] = new List<Action<object>>();
            //eventHandlers[eventType] = new Dictionary<Tuple<object, MethodInfo>, List<Action<object>>>();
        }

        // Wrap the generic handler in a non-generic Action<object>
        Action<object> wrappedHandler = obj => handler(obj as T);
        eventHandlers[eventType].Add(wrappedHandler);

        //var tuple = new Tuple<object, MethodInfo>(handler.Target, handler.Method);
        //if (eventHandlers[eventType].ContainsKey(tuple))
        //{
        //    eventHandlers[eventType][tuple].Add(wrappedHandler);
        //}
        //else
        //{
        //    eventHandlers[eventType].Add(tuple, new List<Action<object>>() { wrappedHandler });
        //}
    }

    public static void Unsubscribe<T>(Action<T> handler) where T : class
    {
        Type eventType = typeof(T);
        if (eventHandlers.ContainsKey(eventType))
        {
            // Find and remove the wrapped handler  ALWAYS FALSE
            eventHandlers[eventType].RemoveAll(
                wrappedHandler =>
                    wrappedHandler.Target == (handler.Target as object) &&
                    wrappedHandler.Method == handler.Method);
            
            //var tuple = new Tuple<object, MethodInfo>(handler.Target, handler.Method);
            //if (eventHandlers[eventType].ContainsKey(tuple))
            //{
            //    eventHandlers[eventType][tuple].Clear();
            //    eventHandlers[eventType].Remove(tuple);
            //}          
        }
    }

    public static void Publish<T>(T eventData) where T : class
    {
        Type eventType = typeof(T);
        if (eventHandlers.ContainsKey(eventType))
        {
            foreach (var handler in eventHandlers[eventType])
            {
                try
                {
                    handler(eventData);
                    //foreach(var h in handler.Value)
                    //    h(eventData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error handling event {eventType.Name} {JsonTool.Serializable(eventData)} : {ex}");
                }
            }
        }
    }

    public static void UnsubscribeAll()
    {
        eventHandlers.Clear();
    }
}