using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace dArtagnan.Unity.UI
{
    public class ConnectionView : MonoBehaviour, IConnectionView
    {
        [SerializeField] private TMP_InputField ipAddressInputField;
        [SerializeField] private Button connectButton;

        public string IpAddress => ipAddressInputField.text;

        public event Action OnConnectButtonClick;

        private void Start()
        {
            connectButton.onClick.AddListener(() => OnConnectButtonClick?.Invoke());
        }
    }
} 