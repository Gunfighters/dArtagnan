using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EventChannel", menuName = "Events/Event Channel")]
public class EventChannel : ScriptableObject
{
    private readonly Dictionary<Type, List<Delegate>> _channels = new();

    public void On<T>(Action<T> action)
    {
        var type = typeof(T);
        Debug.Log($"On {type.Name}: {action.Method.Name}");
        if (!_channels.ContainsKey(typeof(T)))
        {
            _channels[type] = new List<Delegate>();
        }
        _channels[type].Add(action);
    }

    public void Raise<T>(T value)
    {
        var type = value.GetType();
        Debug.Log($"Raising {type.Name}");
        if (_channels.TryGetValue(type, out var channel))
        {
            foreach (var action in channel)
            {
                action.DynamicInvoke(value);
            }
        }
    }
}