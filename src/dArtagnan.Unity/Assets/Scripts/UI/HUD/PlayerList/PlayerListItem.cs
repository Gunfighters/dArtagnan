using System;
using System.Collections.Generic;
using Game.Player.Components;
using R3;
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

        public void Initialize(PlayerCore player)
        {
            name = player.Nickname;
            ID = player.ID;
            gameObject.name = nicknameText.text = player.Nickname;
            var color = player.MyColor;
            color.a = opacity;
            backgroundImage.color = color;
            player.Accuracy.Accuracy.Subscribe(newAcc => accuracyText.text = $"{newAcc}%").AddTo(this);
            player.Health.Alive.Subscribe(newAlive =>
            {
                backgroundImage.sprite = newAlive ? aliveBackground : deadBackground;
                var nicknameTextColor = nicknameText.color;
                nicknameTextColor.a = newAlive ? 1f : 0.5f;
                nicknameText.color = nicknameTextColor;
                accuracyText.enabled = newAlive;
            }).AddTo(this);
        }
    }
}