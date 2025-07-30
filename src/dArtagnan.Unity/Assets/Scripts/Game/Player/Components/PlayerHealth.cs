using dArtagnan.Shared;
using UnityEngine;

namespace Game.Player.Components
{
    public class PlayerHealth : MonoBehaviour
    {
        public bool Alive { get; private set; }
        private PlayerModel _playerModel;

        private void Awake()
        {
            _playerModel = GetComponent<PlayerModel>();
        }

        public void Initialize(PlayerInformation info)
        {
            SetAlive(info.Alive);
        }

        public void SetAlive(bool newAlive)
        {
            Alive = newAlive;
            if (Alive)
                _playerModel.Idle();
            else
                _playerModel.Die();
        }
    }
}