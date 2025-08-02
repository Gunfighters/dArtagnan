using dArtagnan.Shared;
using Game.Player.UI;
using UnityEngine;

namespace Game.Player.Components
{
    public class PlayerDig : MonoBehaviour
    {
        [SerializeField] private CraftSlider slider;
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
            slider.SetProgress(0);
            slider.gameObject.SetActive(dig);
            SetMotion(Digging);
        }
    }
}