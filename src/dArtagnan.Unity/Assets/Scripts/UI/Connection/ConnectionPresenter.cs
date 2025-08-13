using R3;

namespace UI.Connection
{
    public static class ConnectionPresenter
    {
        public static void Initialize(ConnectionView view)
        {
            ConnectionModel.Port.Value = view.config.port;
            view
                .setAwsButton
                .OnClickAsObservable()
                .Subscribe(_ => ConnectionModel.IPEndpoint.Value = view.config.awsHost)
                .AddTo(view);
            view
                .setLocalHostButton
                .OnClickAsObservable()
                .Subscribe(_ => ConnectionModel.IPEndpoint.Value = "localhost")
                .AddTo(view);
            view
                .connectButton
                .OnClickAsObservable()
                .Subscribe(_ => ConnectionModel.Connect())
                .AddTo(view);
            ConnectionModel
                .IsConnecting
                .Subscribe(connecting => view.connectButton.interactable = !connecting)
                .AddTo(view);
        }
    }
}