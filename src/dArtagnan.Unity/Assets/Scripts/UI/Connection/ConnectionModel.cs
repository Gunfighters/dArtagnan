using R3;

namespace UI.Connection
{
    public static class ConnectionModel
    {
        public static readonly ReactiveProperty<string> IPEndpoint = new();
        public static readonly ReactiveProperty<int> Port = new();

        public static void Connect()
        {
            LocalEventChannel.InvokeOnEndpointSelected(IPEndpoint.CurrentValue, Port.CurrentValue);
        }
    }
}