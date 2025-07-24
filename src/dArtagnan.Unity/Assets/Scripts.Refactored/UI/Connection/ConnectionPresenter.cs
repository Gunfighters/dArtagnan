using UnityEngine;

namespace dArtagnan.Unity.UI
{
    public class ConnectionPresenter
    {
        private readonly IConnectionView _view;
        // 추후 구현될 네트워크 매니저에 대한 참조입니다.
        // private readonly dArtagnan.Shared.NetworkManager _networkManager;

        public ConnectionPresenter(IConnectionView view/*, dArtagnan.Shared.NetworkManager networkManager*/)
        {
            _view = view;
            // _networkManager = networkManager;

            _view.OnConnectButtonClick += HandleConnectButtonClick;
        }

        private void HandleConnectButtonClick()
        {
            var ipAddress = _view.IpAddress;
            
            // IP 주소 유효성 검사 (선택 사항)
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                Debug.LogError("IP Address cannot be empty.");
                return;
            }

            // _networkManager.Connect(ipAddress);
        }

        public void Dispose()
        {
            _view.OnConnectButtonClick -= HandleConnectButtonClick;
        }
    }
} 