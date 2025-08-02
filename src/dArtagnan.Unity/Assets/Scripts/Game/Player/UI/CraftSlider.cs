using dArtagnan.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Player.UI
{
    public class CraftSlider : MonoBehaviour
    {
        private Slider _slider;

        private void Awake()
        {
            _slider = GetComponent<Slider>();
            _slider.maxValue = 1;
        }

        private void Update()
        {
            _slider.value += Time.deltaTime / Constants.CREATING_DURATION;
        }

        public void SetProgress(float progress)
        {
            _slider.value = progress;
        }
    }
}