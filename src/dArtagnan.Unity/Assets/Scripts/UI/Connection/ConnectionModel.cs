using R3;

namespace UI.Connection
{
    public static class ConnectionModel
    {
        public static readonly ReactiveProperty<bool> IsConnecting = new(false);
        public static readonly ReactiveProperty<string> IPEndpoint = new();
        public static readonly ReactiveProperty<int> Port = new();

        public static void Connect()
        {
            IsConnecting.Value = true;
            LocalEventChannel.InvokeOnEndpointSelected(IPEndpoint.CurrentValue, Port.CurrentValue);
        }
    }
}