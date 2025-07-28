using System;
using Game.Player;
using Game.Player.Components;
using UnityEngine;

public static class LocalEventChannel
{
    public static event Action<PlayerCore> OnNewCameraTarget;
    public static void InvokeOnNewCameraTarget(PlayerCore playerCore) => OnNewCameraTarget?.Invoke(playerCore);

    public static event Action<PlayerCore, bool> OnNewHost;
    public static void InvokeOnNewHost(PlayerCore newHost, bool youAreHost) => OnNewHost?.Invoke(newHost, youAreHost);

    public static event Action<bool> OnLocalPlayerAlive;
    public static void InvokeOnLocalPlayerAlive(bool alive) => OnLocalPlayerAlive?.Invoke(alive);

    public static event Action<string, int> OnEndpointSelected;
    
    public static void InvokeOnEndpointSelected(string endpoint, int port) => OnEndpointSelected?.Invoke(endpoint, port);

    public static event Action<int> OnLocalPlayerBalanceUpdate;
    
    public static void InvokeOnLocalPlayerBalanceUpdate(int balance) => OnLocalPlayerBalanceUpdate?.Invoke(balance);
}