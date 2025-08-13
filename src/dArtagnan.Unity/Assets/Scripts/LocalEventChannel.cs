using System;
using dArtagnan.Shared;
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

    public static void InvokeOnEndpointSelected(string endpoint, int port) =>
        OnEndpointSelected?.Invoke(endpoint, port);

    public static event Action<int> OnLocalPlayerBalanceUpdate;

    public static void InvokeOnLocalPlayerBalanceUpdate(int balance) => OnLocalPlayerBalanceUpdate?.Invoke(balance);

    public static event Action OnConnectionFailure;
    public static void InvokeOnConnectionFailure() => OnConnectionFailure?.Invoke();

    public static event Action OnConnectionSuccess;
    public static void InvokeOnConnectionSuccess() => OnConnectionSuccess?.Invoke();

    public static event Action BackToConnection;
    public static void InvokeOnBackToConnection() => BackToConnection?.Invoke();

    public static event Action<string, Color> OnAlertMessage;
    public static void InvokeOnAlertMessage(string msg, Color color) => OnAlertMessage?.Invoke(msg, color);

    public static event Action<ItemId> OnLocalPlayerNewItem;
    public static void InvokeOnLocalPlayerNewItem(ItemId itemId) => OnLocalPlayerNewItem?.Invoke(itemId);
}