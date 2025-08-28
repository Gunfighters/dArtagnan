using Game.Player.Data;
using R3;
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

        private void Awake()
        {
            _slider = GetComponent<Slider>();
            _slider.minValue = 0;
        }

        public void Initialize(PlayerInfoModel model)
        {
            model.MinEnergyToShoot.Subscribe(SetThreshold);
            model.EnergyData.Subscribe(data => _slider.maxValue = data.MaxEnergy);
            model.EnergyData.Subscribe(data =>
            {
                _slider.value = data.CurrentEnergy;
                fill.sprite = _slider.value >= model.MinEnergyToShoot.CurrentValue ? loadedFill : normalFill;
            });
        }

        private void SetThreshold(int threshold)
        {
            thresholdMarker.rectTransform.anchoredPosition = new Vector2(36 * (threshold - 1) + 30, 0);
        }
    }
}