using dArtagnan.Shared;
using Game.Player.Data;
using Game.Player.UI;
using UnityEngine;

namespace Game.Player.Components
{
    public class PlayerEnergy : MonoBehaviour
    {
        [SerializeField] private EnergySlider energySlider;

        public void Initialize(PlayerInfoModel model)
        {
            energySlider.Initialize(model);
            EnergyData = model.EnergyData;
            MinEnergyToShoot = model.MinEnergyToShoot;
            energySlider.Initialize(EnergyData.MaxEnergy, MinEnergyToShoot);
            UpdateCurrentEnergy(model.EnergyData.CurrentEnergy);
        }

        private void Update()
        {
            UpdateCurrentEnergy(EnergyData.CurrentEnergy + Constants.ENERGY_RECOVERY_RATE * Time.deltaTime);
        }
    }
}