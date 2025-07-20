using Game;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD
{
    public class SpeedSlider : MonoBehaviour
    {
        private void Start()
        {
            var slider = GetComponent<Slider>();
            slider.onValueChanged.AddListener(PlayerGeneralManager.LocalPlayer.SetSpeed);
            slider.onValueChanged.AddListener(_ => PacketChannel.Raise(PlayerGeneralManager.LocalPlayer.MovementData));
            slider.value = slider.value;
        }
    }
}