using Game;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD
{
    public class RangeSlider : MonoBehaviour
    {
        private void Start()
        {
            var slider = GetComponent<Slider>();
            slider.onValueChanged.AddListener(PlayerGeneralManager.LocalPlayer.SetRange);
            slider.value = slider.value;
        }
    }
}