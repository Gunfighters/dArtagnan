using Networking;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Connection
{
    public class ConnectionView : MonoBehaviour
    {
        [Header("References")] public NetworkManagerConfig config;

        [Header("UI")] public Button connectButton;

        public Button setLocalHostButton;
        public Button setAwsButton;

        private void Awake()
        {
            ConnectionPresenter.Initialize(this);
        }
    }
}