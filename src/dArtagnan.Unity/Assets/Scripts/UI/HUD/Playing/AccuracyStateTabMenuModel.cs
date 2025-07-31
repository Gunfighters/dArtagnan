using System.Linq;
using dArtagnan.Shared;
using Game;
using R3;
using UnityEngine;

namespace UI.HUD.Playing
{
    public static class AccuracyStateTabMenuModel
    {
        public static readonly ReactiveProperty<int> State = new();

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            PacketChannel.On<RoundStartFromServer>(OnGamePlaying);
            PacketChannel.On<PlayerAccuracyStateBroadcast>(OnStateBroadcast);
        }

        private static void OnGamePlaying(RoundStartFromServer e)
        {
            State.Value = e.PlayersInfo
                .Single(i => i.PlayerId == PlayerGeneralManager.LocalPlayerCore.ID)
                .AccuracyState;
        }

        private static void OnStateBroadcast(PlayerAccuracyStateBroadcast e)
        {
            if (PlayerGeneralManager.LocalPlayerCore.ID == e.PlayerId)
                State.Value = e.AccuracyState;
        }
    }
}