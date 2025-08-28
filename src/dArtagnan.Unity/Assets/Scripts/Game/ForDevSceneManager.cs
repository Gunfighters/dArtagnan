using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game
{
    public class ForDevSceneManager : MonoBehaviour
    {
        public void SetHost(string host)
        {
            PlayerPrefs.SetString("GameServerIP", host);
            PlayerPrefs.SetInt("GameServerPort", 7777);
        }

        public void LoadGameScene()
        {
            SceneManager.LoadScene("Game");
        }
    }
}