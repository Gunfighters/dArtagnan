using dArtagnan.Shared;
using UnityEngine;

namespace Game.Player.Components
{
    public class PlayerDig : MonoBehaviour
    {
        private PlayerModel _playerModel;
        public bool Digging { get; private set; }

        private void Awake()
        {
            _playerModel = GetComponent<PlayerModel>();
        }

        private void SetMotion(bool dig)
        {
            if (dig)
                _playerModel.Dig();
            else
                _playerModel.Idle();
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