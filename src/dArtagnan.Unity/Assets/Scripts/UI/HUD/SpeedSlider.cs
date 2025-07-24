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
            slider.onValueChanged.AddListener(PlayerGeneralManager.LocalPlayer.Physics.SetSpeed);
            slider.onValueChanged.AddListener(_ => PacketChannel.Raise(PlayerGeneralManager.LocalPlayer.Physics.MovementData));
            slider.value = slider.value;
        }
    }
}