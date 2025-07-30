using UnityEngine;
using UnityEngine.UI;

namespace Game.Player.UI
{
    public class ReloadingSlider : MonoBehaviour
    {
        private Slider _slider;

        private void Awake()
        {
            _slider = GetComponent<Slider>();
        }

        public void Fill(float ratio)
        {
            _slider.value = _slider.maxValue * (1 - ratio);
        }
    }
}
