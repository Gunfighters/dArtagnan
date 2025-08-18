using System;
using System.Collections.Generic;
using dArtagnan.Shared;
using R3;

/// <summary>
/// 패킷을 주고받는 채널.
/// 각 컴포넌트가 Awake()에서 PacketChannel을 구독하면,
/// 그 후에 NetworkManager가 Start()에서 서버와 연결하여 패킷을 받아왹 시작한다.
/// </summary>
/// <remarks>
/// Awake()에서 PacketChannel.Clear()를 실행하지 말 것.
/// 다른 컴포넌트에서 등록한 리스너가 삭제될 수 있음.
/// </remarks>
public static class PacketChannel
{
    private static readonly Dictionary<Type, Subject<IPacket>> Subjects = new();

    public static void On<T>(Action<T> action) where T : struct, IPacket
    {
        var type = typeof(T);
        if (!Subjects.ContainsKey(type))
        {
            Subjects[type] = new Subject<IPacket>();
        }

        Subjects[type].Subscribe(packet => action((T)packet));
    }

    public static void Raise<T>(T value) where T : IPacket
    {
        var type = value.GetType();
        if (Subjects.TryGetValue(type, out var subject))
        {
            subject.OnNext(value);
        }
    }

    public static void Clear()
    {
        foreach (var subject in Subjects.Values)
        {
            subject?.Dispose();
        }

        Subjects.Clear();
    }
}