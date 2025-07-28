using UnityEngine;
using UnityEngine.UI;

namespace Game.Player.UI
{
    public class ReloadingTimePie : MonoBehaviour
    {
        private Image _cooldownPieImage;

        private void Awake()
        {
            _cooldownPieImage = GetComponent<Image>();
        }

        public void Fill(float ratio)
        {
            _cooldownPieImage.fillAmount = ratio >= 1f ? 0 : 1 - ratio;
        }
    }
}
