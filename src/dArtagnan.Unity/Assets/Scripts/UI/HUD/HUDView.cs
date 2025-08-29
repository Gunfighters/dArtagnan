using UnityEngine;

namespace UI.HUD
{
    public class HUDView : MonoBehaviour
    {
        [Header("Canvases")] public Canvas controls;

        public Canvas spectating;
        public Canvas waiting;
        public Canvas playing;
        public Canvas inRound;
        public Canvas isHost;

        private void Start()
        {
            HUDPresenter.Initialize(this, new HUDModel());
        }
    }
}