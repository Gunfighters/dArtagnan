using dArtagnan.Shared;
using UnityEngine;

namespace Game.Player.Components
{
    public class PlayerHealth : MonoBehaviour
    {
        public bool Alive { get; private set; }
        private PlayerCore _core;

        private void Awake()
        {
            _core = GetComponent<PlayerCore>();
        }

        public void Initialize(PlayerInformation info)
        {
            SetAlive(info.Alive);
        }

        public void SetAlive(bool newAlive)
        {
            Alive = newAlive;
            if (Alive)
                _core.Model.Idle();
            else
            {
                _core.Model.Die();
                _core.Reload.OnDeath();
            }
        }
    }
}