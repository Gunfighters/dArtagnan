using Game.Player.Data;
using Game.Player.UI;
using UnityEngine;

namespace Game.Player.Components
{
    public class PlayerEnergy : MonoBehaviour
    {
        [SerializeField] private EnergySlider energySlider;

        public void Initialize(PlayerModel model) => energySlider.Initialize(model);
    }
}