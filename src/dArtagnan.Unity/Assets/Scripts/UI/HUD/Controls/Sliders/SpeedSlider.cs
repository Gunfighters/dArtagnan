using Game;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD.Controls.Sliders
{
    public class SpeedSlider : MonoBehaviour
    {
        private void Start()
        {
            var slider = GetComponent<Slider>();
            slider.onValueChanged.AddListener(GameService.LocalPlayer.Physics.SetSpeed);
            slider.onValueChanged.AddListener(_ => PacketChannel.Raise(GameService.LocalPlayer.Physics.MovementData));
            slider.value = slider.value;
        }
    }
}