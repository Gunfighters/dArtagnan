using dArtagnan.Shared;
using UnityEngine;

namespace Game.Player.Components
{
    public class PlayerDig : MonoBehaviour
    {
        private PlayerCore _core;
        public bool Digging { get; private set; }

        private void Awake()
        {
            _core = GetComponent<PlayerCore>();
        }

        private void SetMotion(bool dig)
        {
            if (dig)
                _core.Model.Dig();
            else
                _core.Model.Idle();
        }

        public void Initialize(PlayerInformation info)
        {
            ToggleDigging(info.IsCreatingItem);
        }

        public void ToggleDigging(bool dig)
        {
            Digging = dig;
            SetMotion(Digging);
        }
    }
}