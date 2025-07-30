using dArtagnan.Shared;
using R3;
using UnityEditor;

namespace UI.HUD.InRound.StakeBoard
{
    [InitializeOnLoad]
    public static class StakeBoardModel
    {
        public static readonly ReactiveProperty<int> Amount = new();

        static StakeBoardModel()
        {
            PacketChannel.On<RoundStartFromServer>(e =>
            {
                Amount.Value = 0;
            });
            PacketChannel.On<BettingDeductionBroadcast>(e =>
            {
                Amount.Value = e.TotalPrizeMoney;
            });
        }
    }
}