using R3;

namespace UI.Connection
{
    public class ConnectionViewModel
    {
        private readonly ConnectionModel _model;

        public ConnectionViewModel(ConnectionModel model)
        {
            _model = model;
        }

        public ReadOnlyReactiveProperty<string> IPEndpoint => _model.ipEndpoint;
        public ReadOnlyReactiveProperty<int> Port => _model.port;
        public ReadOnlyReactiveProperty<string> AwsEndpoint => _model.awsEndpoint;

        public void Connect()
        {
            LocalEventChannel.InvokeOnEndpointSelected(IPEndpoint.CurrentValue, Port.CurrentValue);
        }

        public void SetEndpoint(string ip)
        {
            _model.ipEndpoint.Value = ip;
        }

        public void SetEndpointToAws()
        {
            _model.ipEndpoint.Value = AwsEndpoint.CurrentValue;
        }

        public void SetEndpointToLocalhost()
        {
            _model.ipEndpoint.Value = "localhost";
        }
    }
}