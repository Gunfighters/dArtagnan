using dArtagnan.Shared;
using Game.Misc;
using TMPro;
using UnityEngine;

namespace Game.Player.Components
{
    public class PlayerCore : MonoBehaviour
    {
        public int ID { get; private set; }
        public string Nickname { get; private set; }
        public TextMeshProUGUI nicknameText;
        [SerializeField] private ColorPool colorPool;
        public ModelManager ModelManager { get; private set; }
        public PlayerHealth Health { get; private set; }
        public PlayerPhysics Physics { get; private set; }
        public PlayerShoot Shoot { get; private set; }
        public PlayerAccuracy Accuracy { get; private set; }
        public PlayerReload Reload { get; private set; }
        public PlayerBalance Balance { get; private set; }

        public Color MyColor => colorPool.colors[ID - 1];

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

        public void Initialize(PlayerInformation info)
        {
            ID = info.PlayerId;
            SetNickname(info.Nickname);
            SetColor(MyColor);
            ModelManager.Initialize(info);
            Health.Initialize(info);
            Physics.Initialize(info);
            Shoot.Initialize(info);
            Accuracy.Initialize(info);
            Reload.Initialize(info);
            Balance.Initialize(info);
        }
    }
}