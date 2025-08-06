using dArtagnan.Shared;
using Game.Misc;
using TMPro;
using UnityEngine;

namespace Game.Player.Components
{
    public class PlayerCore : MonoBehaviour
    {
        [SerializeField] private ColorPool colorPool;
        public int ID { get; private set; }
        public string Nickname { get; private set; }
        public PlayerModel Model { get; private set; }
        public PlayerHealth Health { get; private set; }
        public PlayerPhysics Physics { get; private set; }
        public PlayerShoot Shoot { get; private set; }
        public PlayerAccuracy Accuracy { get; private set; }
        public PlayerEnergy Energy { get; private set; }
        public PlayerBalance Balance { get; private set; }
        public PlayerTrajectory Trajectory { get; private set; }

        public PlayerDig Dig { get; private set; }

        public Color MyColor => colorPool.colors[ID - 1];

        private void Awake()
        {
            Model = GetComponent<PlayerModel>();
            Health = GetComponent<PlayerHealth>();
            Physics = GetComponent<PlayerPhysics>();
            Shoot = GetComponent<PlayerShoot>();
            Accuracy = GetComponent<PlayerAccuracy>();
            Energy = GetComponent<PlayerEnergy>();
            Balance = GetComponent<PlayerBalance>();
            Trajectory = GetComponent<PlayerTrajectory>();
            Dig = GetComponent<PlayerDig>();
        }

        private void SetNickname(string newNickname)
        {
            Nickname = newNickname;
        }

        private void SetColor(Color color)
        {
            Model.SetColor(color);
        }

        public void Initialize(PlayerInformation info)
        {
            ID = info.PlayerId;
            SetNickname(info.Nickname);
            SetColor(MyColor);
            Model.Initialize(info);
            Health.Initialize(info);
            Physics.Initialize(info);
            Shoot.Initialize(info);
            Accuracy.Initialize(info);
            Energy.Initialize(info);
            Balance.Initialize(info);
            Trajectory.Initialize(info);
            Dig.Initialize(info);
        }
    }
}