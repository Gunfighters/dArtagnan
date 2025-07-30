using Assets.HeroEditor4D.Common.Scripts.Common;
using R3;
using UnityEngine;

namespace UI.HUD
{
    public class HUDView : MonoBehaviour
    {
        [Header("Canvases")]
        public Canvas controls;
        public Canvas spectating;
        public Canvas waiting;
        public Canvas playing;
        public Canvas inRound;
        public Canvas isHost;

        private void Awake()
        {
            HUDPresenter.Initialize(this);
        }
    }
}