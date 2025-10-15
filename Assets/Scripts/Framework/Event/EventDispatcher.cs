using System;
using System.Collections;
using System.Collections.Generic;

public static class EventDispatcher<T>
{
    private static readonly Dictionary<string, Action<T>> _eventTable = new();

    public static void addListener(string eventName, Action<T> callback)
    {
        if (!_eventTable.ContainsKey(eventName))
        {
            _eventTable[eventName] = delegate { };
        }
        _eventTable[eventName] += callback;
    }

    public static void removeListener(string eventName, Action<T> callback)
    {
        if (_eventTable.ContainsKey(eventName))
        {
            _eventTable[eventName] -= callback;
            if (_eventTable[eventName] == null)
            {
                _eventTable.Remove(eventName);
            }
        }
    }

    public static void triggerEvent(string eventName, T arg)
    {
        if (_eventTable.ContainsKey(eventName))
        {
            _eventTable[eventName]?.Invoke(arg);
        }
    }

    public static void clear()
    {
        _eventTable.Clear();
    }
}
