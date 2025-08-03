using UnityEngine;
using UnityEngine.UI;

namespace Game.Player.UI
{
    public class ReloadingSlider : MonoBehaviour
    {
        private Slider _slider;
        [SerializeField] private Image fill;

        private void Awake()
        {
            _slider = GetComponent<Slider>();
        }

        public void Fill(float ratio)
        {
            var progress = Mathf.Clamp01(1 - ratio);
            _slider.value = _slider.maxValue * progress;
            fill.color = progress switch
            {
                < 0.8f => Color.grey,
                < 1f => Color.yellow,
                1f => Color.red,
                _ => Color.white
            };
        }
    }
}