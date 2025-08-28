using dArtagnan.Shared;
using Game.Player.Components;
using R3;
using R3.Triggers;
using UnityEngine;

namespace Game.Player.Data
{
    public static class PlayerPresenter
    {
        public static void Initialize(PlayerInfoModel model, PlayerCore view)
        {
            view.Initialize(model);
            view.FixedUpdateAsObservable()
                .Subscribe(_ => model.Position.Value = model.NextPosition())
                .AddTo(view);
            view.UpdateAsObservable()
                .Subscribe(_ =>
                {
                    var data = model.EnergyData.Value;
                    data.CurrentEnergy += Time.deltaTime * Constants.ENERGY_RECOVERY_RATE;
                    model.EnergyData.Value = data;
                })
                .AddTo(view);
        }
    }
}