using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Player.UI
{
    public class SliderPrefix : MonoBehaviour
    {
        [SerializeField] private Slider slider;
        [SerializeField] private TextMeshProUGUI text;

        private void Update()
        {
            text.text = Mathf.FloorToInt(slider.value).ToString();
        }
    }
}