using R3;
using UnityEngine;

namespace UI.Connection
{
    public static class ConnectionPresenter
    {
        public static void Initialize(ConnectionView view)
        {
            ConnectionModel.Port.Value = view.config.port;
            view
                .ipEndpointInputField
                .OnValueChangedAsObservable()
                .Subscribe(ConnectionModel.IPEndpoint.AsObserver());
            view
                .setAwsButton
                .OnClickAsObservable()
                .Subscribe(_ => ConnectionModel.IPEndpoint.Value = view.config.awsHost);
            view
                .setLocalHostButton
                .OnClickAsObservable()
                .Subscribe(_ => ConnectionModel.IPEndpoint.Value = "localhost");
            view
                .connectButton
                .OnClickAsObservable()
                .Subscribe(_ => ConnectionModel.Connect());
            ConnectionModel
                .IPEndpoint
                .Subscribe(value => view.ipEndpointInputField.text = value);
            ConnectionModel
                .IsConnecting
                .Subscribe(connecting => view.connectButton.interactable = !connecting);
        }
    }
}