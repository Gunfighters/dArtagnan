using System;

public static class LocalEventChannel
{
    public static event Action<Player> OnNewCameraTarget;
    public static void InvokeOnNewCameraTarget(Player player) => OnNewCameraTarget?.Invoke(player);

    public static event Action<Player, bool> OnNewHost;
    public static void InvokeOnNewHost(Player newHost, bool youAreHost) => OnNewHost?.Invoke(newHost, youAreHost);

    public static event Action<bool> OnLocalPlayerAlive;
    public static void InvokeOnLocalPlayerAlive(bool alive) => OnLocalPlayerAlive?.Invoke(alive);
}