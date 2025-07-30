using Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Connection
{
    public class ConnectionView : MonoBehaviour
    {
        [Header("References")]
        public NetworkManagerConfig config;
        
        [Header("UI")]
        public TMP_InputField ipEndpointInputField; 
        public Button connectButton; 
        public Button setLocalHostButton; 
        public Button setAwsButton;
        
        private void Awake()
        {
            ConnectionPresenter.Initialize(this);
        }
    }
}