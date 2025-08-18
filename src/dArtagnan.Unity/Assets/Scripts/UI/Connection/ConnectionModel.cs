using R3;
using UnityEngine;

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
        }
    }
}