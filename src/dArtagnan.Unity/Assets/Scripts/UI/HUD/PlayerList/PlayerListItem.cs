using Game.Player.Data;
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
        public PlayerModel PlayerModel { get; private set; }
        
        public void Initialize(PlayerModel model)
        {
            PlayerModel = model;
            model.Nickname.Subscribe(newNick => name = nicknameText.text = newNick);
            var color = model.Color;
            backgroundImage.color = color;
            model.Accuracy.Subscribe(newAcc => accuracyText.text = $"{newAcc}%").AddTo(this);
            model.Alive.Subscribe(newAlive =>
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