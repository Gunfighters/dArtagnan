using UnityEngine;
using UnityEngine.UI;

namespace UI.NetworkFailure
{
    public class NetworkFailureConfirmButton : MonoBehaviour
    {
        private Button _btn;

        private void Awake()
        {
            _btn = GetComponent<Button>();
            _btn.onClick.AddListener(LocalEventChannel.InvokeOnBackToConnection);
        }
    }
}