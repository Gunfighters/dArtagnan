using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI.NetworkFailure
{
    public class NetworkFailureConfirmButton : MonoBehaviour
    {
        private Button _btn;

        private void Awake()
        {
            _btn = GetComponent<Button>();
            _btn.onClick.AddListener(() => SceneManager.LoadScene("Lobby"));
        }
    }
}