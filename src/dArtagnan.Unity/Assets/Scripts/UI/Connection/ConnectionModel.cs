using R3;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI.Connection
{
    public static class ConnectionModel
    {
        public static readonly ReactiveProperty<bool> IsConnecting = new(false);
        public static readonly ReactiveProperty<string> IPEndpoint = new();
        public static readonly ReactiveProperty<int> Port = new();

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            LocalEventChannel.OnConnectionFailure += () => IsConnecting.Value = false;
            LocalEventChannel.OnConnectionSuccess += () => SceneManager.LoadScene("Game");
        }

        public static void Connect()
        {
            IsConnecting.Value = true;
            LocalEventChannel.InvokeOnEndpointSelected(IPEndpoint.CurrentValue, Port.CurrentValue);
        }
    }
}