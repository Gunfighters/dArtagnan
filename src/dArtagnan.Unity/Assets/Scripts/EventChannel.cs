using System;
using System.Collections.Generic;

public class EventChannel<TEventType>
{
    private readonly Dictionary<Type, List<Delegate>> _channels = new();
    public static EventChannel<TEventType> Instance => _instance ??= new EventChannel<TEventType>();

    private static EventChannel<TEventType> _instance;

    public void On<T>(Action<T> action) where T : TEventType
    {
        var type = typeof(T);
        if (!_channels.ContainsKey(typeof(T)))
        {
            _channels[type] = new List<Delegate>();
        }
        _channels[type].Add(action);
    }

    public void Raise<T>(T value) where T : TEventType
    {
        var type = value.GetType();
        if (_channels.TryGetValue(type, out var channel))
        {
            foreach (var action in channel)
            {
                action.DynamicInvoke(value);
            }
        }
    }
}