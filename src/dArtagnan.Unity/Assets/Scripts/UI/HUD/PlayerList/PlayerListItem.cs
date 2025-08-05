using Game.Player.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD.PlayerList
{
    public class PlayerListItem : MonoBehaviour
    {
        public int ID { get; private set; }
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI accuracyText;
        [SerializeField] private TextMeshProUGUI nicknameText;
        [SerializeField] private Sprite aliveBackground;
        [SerializeField] private Sprite deadBackground;
        [Range(0, 1)] [SerializeField] private float opacity;

        public void Setup(PlayerCore info)
        {
            ID = info.ID;
            accuracyText.text = $"{info.Accuracy.Accuracy}%";
            nicknameText.text = info.Nickname;
            backgroundImage.sprite = info.Health.Alive ? aliveBackground : deadBackground;
            var nicknameTextColor = nicknameText.color;
            nicknameTextColor.a = info.Health.Alive ? 1f : 0.5f;
            nicknameText.color = nicknameTextColor;
            var accuracyTextColor = accuracyText.color;
            accuracyTextColor.a = info.Health.Alive ? 1f : 0.5f;
            accuracyText.color = accuracyTextColor;
            if (info.Health.Alive)
            {
                var color = backgroundImage.color = info.MyColor;
                color.a = opacity;
                backgroundImage.color = color;
            }
        }
    }
}