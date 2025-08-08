using UnityEngine;
using UnityEngine.UI;

namespace Game.Player.UI
{
    public class EnergySlider : MonoBehaviour
    {
        [SerializeField] private Image fill;
        [SerializeField] private Image thresholdMarker;
        [SerializeField] private Sprite normalFill;
        [SerializeField] private Sprite loadedFill;
        [SerializeField] private RectTransform ruler;
        private Slider _slider;
        private int _threshold;

        private void Awake()
        {
            _slider = GetComponent<Slider>();
        }

        public void Initialize(int max, int threshold)
        {
            _slider.minValue = 0;
            _slider.maxValue = max;
            SetThreshold(threshold);
        }

        public void SetThreshold(int threshold)
        {
            _threshold = threshold;
            thresholdMarker.rectTransform.anchoredPosition = new Vector2(36 * threshold + 30, 0);
        }

        public void SetMax(int max)
        {
            _slider.maxValue = max;
        }

        public void Fill(float newCurrentEnergy)
        {
            _slider.value = newCurrentEnergy;
            fill.sprite = _slider.value >= _threshold ? loadedFill : normalFill;
        }
    }
}