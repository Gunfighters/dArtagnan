using System;
using System.Collections.Generic;
using dArtagnan.Shared;
using UnityEngine;

public static class PacketChannel
{
    private static readonly Dictionary<Type, List<Action<IPacket>>> Channels = new();

    public static void On<T>(Action<T> action) where T : struct, IPacket
    {
        var type = typeof(T);
        if (!Channels.ContainsKey(typeof(T)))
        {
            Channels[type] = new ();
        }

        Channels[type].Add(Wrapper);
        return;

        void Wrapper(IPacket packet) => action.Invoke((T)packet);
    }

    public static void Raise<T>(T value) where T : IPacket
    {
        var type = value.GetType();
        if (Channels.TryGetValue(type, out var channel))
        {
            foreach (var action in channel)
            {
                Debug.Log($"Raise {type.Name} -> {action.Method.Name}");
                action.Invoke(value);
            }
        }
    }
}