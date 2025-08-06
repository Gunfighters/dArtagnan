using System;
using System.Collections.Generic;
using System.Linq;
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
            Channels[type] = new();
        }

        Channels[type].Add(Wrapper);
        return;

        void Wrapper(IPacket packet)
        {
            // Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] Raise {packet.GetType()} : {action.Method.DeclaringType}.{action.Method.Name}");
            action.Invoke((T)packet);
        }
    }

    public static void Raise<T>(T value) where T : IPacket
    {
        var type = value.GetType();
        if (Channels.TryGetValue(type, out var channel))
        {
            List<Action<IPacket>> run = new();
            bool modified;
            do
            {
                modified = false;
                foreach (var action in channel.ToArray().Where(a => !run.Contains(a)))
                {
                    action.Invoke(value);
                    run.Add(action);
                    modified = true;
                }
            } while (modified);
        }
    }
}