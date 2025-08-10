using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD.Spectating.Carousel
{
    public class SpectatingCarouselView : MonoBehaviour
    {
        public Button leftButton;
        public Button rightButton;
        public Image colorSlot;

        private void Awake()
        {
            SpectatingCarouselPresenter.Initialize(this);
        }
    }
}