using UnityEngine;

namespace UI.HUD.Splashes
{
    public class SplashView : MonoBehaviour
    {
        [Header("Splashes")]
        // public GameObject gameStartSplash;
        // public GameObject gameOverSplash;
        public GameObject roundStartSplash;
        public GameObject roundOverSplash;

        private void Awake()
        {
            SplashPresenter.Initialize(this);
        }
    }
}