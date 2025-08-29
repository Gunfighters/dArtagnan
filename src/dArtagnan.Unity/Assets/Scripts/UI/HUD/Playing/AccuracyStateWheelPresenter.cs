using System;
using dArtagnan.Shared;
using Game;
using R3;

namespace UI.HUD.Playing
{
    public static class AccuracyStateWheelPresenter
    {
        public static void Initialize(AccuracyStateWheelView view)
        {
            AccuracyStateWheelModel.State.Subscribe(newState =>
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
                GameService.LocalPlayer.AccuracyState.Value = newState;
                PacketChannel.Raise(new UpdateAccuracyStateFromClient { AccuracyState = newState });
            };
        }
    }
}