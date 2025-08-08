using Game.Player.Components;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD.PlayerList
{
    public class PlayerListItem : MonoBehaviour
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI accuracyText;
        [SerializeField] private TextMeshProUGUI nicknameText;
        [Range(0, 1)] [SerializeField] private float aliveOpacity;
        [Range(0, 1)] [SerializeField] private float deadOpacity;
        public int ID { get; private set; }

        public void Initialize(PlayerCore player)
        {
            name = player.Nickname;
            ID = player.ID;
            gameObject.name = nicknameText.text = player.Nickname;
            var color = player.MyColor;
            backgroundImage.color = color;
            player.Accuracy.Accuracy.Subscribe(newAcc => accuracyText.text = $"{newAcc}%").AddTo(this);
            player.Health.Alive.Subscribe(newAlive =>
            {
                backgroundImage.color = newAlive ? color : Color.grey;
                var nicknameTextColor = nicknameText.color;
                nicknameTextColor.a = newAlive ? aliveOpacity : deadOpacity;
                nicknameText.color = nicknameTextColor;
                accuracyText.enabled = newAlive;
            }).AddTo(this);
        }
    }
}