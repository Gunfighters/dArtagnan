using System.Linq;
using dArtagnan.Shared;
using Game;
using R3;
using UnityEngine;

namespace UI.HUD.Playing
{
    public static class AccuracyStateWheelModel
    {
        public static readonly ReactiveProperty<int> State = new();

        public static void Initialize()
        {
            PacketChannel.On<RoundStartFromServer>(OnGamePlaying);
            PacketChannel.On<UpdateAccuracyStateBroadcast>(OnStateBroadcast);
        }

        private static void OnGamePlaying(RoundStartFromServer e)
        {
            State.Value = e.PlayersInfo
                .Single(i => i.PlayerId == GameService.LocalPlayer.ID.CurrentValue)
                .AccuracyState;
        }

        private static void OnStateBroadcast(UpdateAccuracyStateBroadcast e)
        {
            if (GameService.LocalPlayer.ID.CurrentValue == e.PlayerId)
                State.Value = e.AccuracyState;
        }
    }
}