using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Connection
{
    public class ConnectionView : MonoBehaviour
    {
        private ConnectionViewModel _viewModel;
        [Header("References")]
        [SerializeField] private ConnectionModel model;
        [Header("UI")]
        [SerializeField] private TMP_InputField ipEndpointInputField;
        [SerializeField] private Button connectButton;
        [SerializeField] private Button setLocalHostButton;
        [SerializeField] private Button setAwsButton;
        private void Awake()
        {
            _viewModel = new ConnectionViewModel(model);
            ipEndpointInputField.text = _viewModel.IPEndpoint.ToString();
            _viewModel.IPEndpoint.Subscribe(next => ipEndpointInputField.text = next);
            connectButton.onClick.AddListener(_viewModel.Connect);
            setLocalHostButton.onClick.AddListener(_viewModel.SetEndpointToLocalhost);
            setAwsButton.onClick.AddListener(_viewModel.SetEndpointToAws);
            ipEndpointInputField.OnValueChangedAsObservable().Subscribe(_viewModel.SetEndpoint);
        }
    }
}