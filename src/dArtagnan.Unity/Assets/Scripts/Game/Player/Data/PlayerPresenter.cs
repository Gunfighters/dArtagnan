using dArtagnan.Shared;
using Game.Player.Components;
using R3;
using UnityEngine;

namespace Game.Player.Data
{
    public static class PlayerPresenter
    {
        public static void Initialize(PlayerModel model, PlayerView view)
        {
            view.Initialize(model);
            Observable
                .EveryUpdate()
                .Subscribe(_ =>
                {
                    var data = model.EnergyData.Value;
                    data.CurrentEnergy += Time.deltaTime * Constants.ENERGY_RECOVERY_RATE;
                    model.EnergyData.Value = data;
                });
        }
    }
}