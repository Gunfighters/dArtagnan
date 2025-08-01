using dArtagnan.Shared;
using TMPro;
using UnityEngine;

namespace Game.Player.Components
{
    public class PlayerAccuracy : MonoBehaviour
    {
        public int Accuracy { get; private set; }
        public int AccuracyState { get; private set; }
        [SerializeField] private TextMeshProUGUI accuracyText;

        public void Initialize(PlayerInformation info)
        {
            SetAccuracy(info.Accuracy);
            SetAccuracyState(info.AccuracyState);
        }

        public void SetAccuracy(int newAccuracy)
        {
            Accuracy = newAccuracy;
            accuracyText.text = $"{Accuracy}%";
        }

        public void SetAccuracyState(int newAccuracyState)
        {
            AccuracyState = newAccuracyState;
            accuracyText.color = AccuracyState switch
            {
                1 => new Color32(250, 85, 50, 255),
                -1 => new Color32(75, 150, 220, 255),
                _ => Color.white
            };
        }
    }
}