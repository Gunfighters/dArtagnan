using dArtagnan.Shared;
using Game.Player.Data;
using R3;
using TMPro;
using UnityEngine;

namespace Game.Player.Components
{
    public class PlayerAccuracy : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI accuracyText;

        public void Initialize(PlayerInfoModel model)
        {
            model.Accuracy.Subscribe(SetAccuracy);
            model.AccuracyState.Subscribe(SetAccuracyState);
        }

        public void SetAccuracy(int newAccuracy)
        {
            accuracyText.text = $"{newAccuracy}%";
        }

        public void SetAccuracyState(int newAccuracyState)
        {
            accuracyText.color = newAccuracyState switch
            {
                1 => new Color32(250, 85, 50, 255),
                -1 => new Color32(75, 150, 220, 255),
                _ => Color.white
            };
        }
    }
}