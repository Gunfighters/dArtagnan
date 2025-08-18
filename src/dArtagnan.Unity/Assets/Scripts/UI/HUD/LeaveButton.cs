using R3;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI.HUD
{
    [RequireComponent(typeof(Button))]
    public class LeaveButton : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Button>().OnClickAsObservable().Subscribe(_ => BackToLobby()).AddTo(this);
        }

        private void BackToLobby()
        {
            SceneManager.LoadScene("Lobby");
        }
    }
}