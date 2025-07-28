using dArtagnan.Shared;
using TMPro;
using UnityEngine;

namespace Game.Player.Components
{
    public class PlayerCore : MonoBehaviour
    {
        public int ID { get; private set; }
        public string Nickname { get; private set; }
        public TextMeshProUGUI nicknameText;
        public ModelManager ModelManager { get; private set; }
        public PlayerHealth Health { get; private set; }
        public PlayerPhysics Physics { get; private set; }
        public PlayerShoot Shoot { get; private set; }
        public PlayerAccuracy Accuracy { get; private set; }
        public PlayerReload Reload { get; private set; }
        public PlayerBalance Balance { get; private set; }

        private static readonly Color[] PlayerColors = {
            new(1f, 0.3f, 0.3f),   // 밝은 빨강 - ID 1
            new(0.4f, 0.7f, 1f),   // 밝은 파랑 - ID 2  
            new(0.4f, 1f, 0.4f),   // 밝은 초록 - ID 3
            new(1f, 0.8f, 0.2f),   // 밝은 주황 - ID 4 (노란색 대체)
            new(1f, 0.4f, 1f),     // 밝은 자홍 - ID 5
            new(0.4f, 1f, 1f),     // 밝은 시안 - ID 6
            new(1f, 0.6f, 0.2f),   // 따뜻한 주황 - ID 7
            new(0.8f, 0.4f, 1f)    // 밝은 보라 - ID 8
        };

        public Color MyColor => ID is >= 1 and <= 8 ? PlayerColors[ID - 1] : Color.white;

        private void Awake()
        {
            ModelManager = GetComponent<ModelManager>();
            Health = GetComponent<PlayerHealth>();
            Physics = GetComponent<PlayerPhysics>();
            Shoot = GetComponent<PlayerShoot>();
            Accuracy = GetComponent<PlayerAccuracy>();
            Reload = GetComponent<PlayerReload>();
            Balance = GetComponent<PlayerBalance>();
        }

        private void SetNickname(string newNickname)
        {
            Nickname = newNickname;
            nicknameText.text = Nickname;
        }

        private void SetColor(Color color)
        {
            nicknameText.color = color;
            ModelManager.SetHatColor(color);
        }

        public void Initialize(PlayerInformation info, bool isRemotePlayer = false)
        {
            ID = info.PlayerId;
            SetNickname(info.Nickname);
            SetColor(MyColor);
            ModelManager.Initialize(info);
            Health.Initialize(info);
            Physics.Initialize(info, isRemotePlayer);
            Shoot.Initialize(info);
            Accuracy.Initialize(info);
            Reload.Initialize(info);
            Balance.Initialize(info);
        }
    }
}