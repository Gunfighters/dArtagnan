using System.Collections.Generic;
using dArtagnan.Shared;
using R3;
using UnityEditor;

namespace UI.AccuracySelection
{
    [InitializeOnLoad]
    public static class AccuracySelectionModel
    {
        public static readonly ReactiveProperty<List<int>> Pool = new();
        public static readonly ReactiveProperty<PlayerHasSelectedAccuracyFromServer> LastTaken = new();
        public static readonly ReactiveProperty<int> Turn = new();

        static AccuracySelectionModel()
        {
            PacketChannel.On<AccuracySelectionStartFromServer>(OnSelectionStart);
            PacketChannel.On<PlayerHasSelectedAccuracyFromServer>(OnPlayerSelect);
            PacketChannel.On<PlayerTurnToSelectAccuracy>(OnTurn);
        }

        private static void OnSelectionStart(AccuracySelectionStartFromServer e)
        {
            Pool.Value = e.AccuracyPool;
        }

        private static void OnPlayerSelect(PlayerHasSelectedAccuracyFromServer e)
        {
            LastTaken.Value = e;
        }

        private static void OnTurn(PlayerTurnToSelectAccuracy e)
        {
            Turn.Value = e.PlayerId;
        }
    }
}