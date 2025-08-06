using dArtagnan.Shared;
using Game.Player.UI;
using UnityEngine;

namespace Game.Player.Components
{
    public class PlayerEnergy : MonoBehaviour
    {
        public EnergyData EnergyData { get; private set; }
        public int MinEnergyToShoot { get; private set; }
        [SerializeField] private EnergySlider energySlider;

        public void Initialize(PlayerInformation info)
        {
            EnergyData = info.EnergyData;
            MinEnergyToShoot = info.MinEnergyToShoot;
            energySlider.Initialize(EnergyData.MaxEnergy, MinEnergyToShoot);
            UpdateCurrentEnergy(info.EnergyData.CurrentEnergy);
        }

        private void Update()
        {
            UpdateCurrentEnergy(EnergyData.CurrentEnergy + Constants.ENERGY_RECOVERY_RATE * Time.deltaTime);
        }

        public void UpdateCurrentEnergy(float newCurrentEnergy)
        {
            var data = EnergyData;
            data.CurrentEnergy = Mathf.Min(data.MaxEnergy, newCurrentEnergy);
            EnergyData = data;
            energySlider.Fill(EnergyData.CurrentEnergy);
        }

        public void UpdateMaxEnergy(int newMaxEnergy)
        {
            var data = EnergyData;
            data.MaxEnergy = newMaxEnergy;
            EnergyData = data;
            energySlider.SetMax(newMaxEnergy);
        }

        public void SetThreshold(int threshold)
        {
            MinEnergyToShoot = threshold;
            energySlider.SetThreshold(MinEnergyToShoot);
        }
    }
}