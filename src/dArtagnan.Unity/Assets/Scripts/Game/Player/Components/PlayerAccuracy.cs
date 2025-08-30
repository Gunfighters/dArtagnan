using dArtagnan.Shared;
using Game.Player.Data;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Player.Components
{
    public class PlayerAccuracy : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI accuracyText;
        [SerializeField] private Image accuracyStateIcon;
        [SerializeField] private Sprite upIcon;
        [SerializeField] private Sprite keepIcon;
        [SerializeField] private Sprite downIcon;
        [SerializeField] private Color upColor;
        [SerializeField] private Color keepColor;
        [SerializeField] private Color downColor;

        public void Initialize(PlayerModel model)
        {
            model.Accuracy.Subscribe(SetAccuracy);
            model.AccuracyState.Subscribe(SetAccuracyState);
        }

        private void SetAccuracy(int newAccuracy)
        {
            accuracyText.text = $"{newAccuracy}%";
        }

        private void SetAccuracyState(int newAccuracyState)
        {
            accuracyStateIcon.color = newAccuracyState switch
            {
                1 => upColor,
                -1 => downColor,
                _ => keepColor
            };
            accuracyStateIcon.sprite = newAccuracyState switch
            {
                1 => upIcon,
                -1 => downIcon,
                _ => keepIcon
            };
        }
    }
}