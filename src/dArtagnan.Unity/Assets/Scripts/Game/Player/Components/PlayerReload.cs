using dArtagnan.Shared;
using Game.Player.UI;
using UnityEngine;

namespace Game.Player.Components
{
    public class PlayerReload : MonoBehaviour
    {
        public float TotalReloadTime { get; private set; }
        public float RemainingReloadTime { get; private set; }
        [SerializeField] private ReloadingSlider reloadingSlider;

        public void Initialize(PlayerInformation info)
        {
            TotalReloadTime = info.TotalReloadTime;
            UpdateRemainingReloadTime(info.RemainingReloadTime);
        }

        private void Update()
        {
            UpdateRemainingReloadTime(RemainingReloadTime - Time.deltaTime);
        }

        public void UpdateRemainingReloadTime(float reloadTime)
        {
            RemainingReloadTime = Mathf.Max(0, reloadTime);
            reloadingSlider.Fill(RemainingReloadTime / TotalReloadTime);
        }

        public void OnDeath()
        {
            RemainingReloadTime = TotalReloadTime;
        }
    }
}