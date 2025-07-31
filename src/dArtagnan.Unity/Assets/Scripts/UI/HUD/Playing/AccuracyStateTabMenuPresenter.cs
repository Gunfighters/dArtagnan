using System;
using dArtagnan.Shared;
using Game;
using R3;

namespace UI.HUD.Playing
{
    public static class AccuracyStateTabMenuPresenter
    {
        public static void Initialize(AccuracyStateTabMenuView view)
        {
            AccuracyStateTabMenuModel.State.Subscribe(newState =>
            {
                var activated = newState switch
                {
                    1 => view.up,
                    0 => view.keep,
                    -1 => view.down,
                    _ => throw new Exception($"Unexpected state: {newState}")
                };
                view.SwitchUIOnly(activated);
            });
            view.OnSwitch += newState =>
            {
                PlayerGeneralManager.LocalPlayerCore?.Accuracy.SetAccuracyState(newState);
                PacketChannel.Raise(new SetAccuracyState { AccuracyState = newState });
            };
        }
    }
}