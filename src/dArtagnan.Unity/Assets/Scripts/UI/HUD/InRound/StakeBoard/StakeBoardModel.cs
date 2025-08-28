using dArtagnan.Shared;
using R3;

namespace UI.HUD.InRound.StakeBoard
{
    public static class StakeBoardModel
    {
        public static readonly ReactiveProperty<int> Amount = new();

        public static void Initialize()
        {
            PacketChannel.On<RoundStartFromServer>(e => { Amount.Value = 0; });
            PacketChannel.On<BettingDeductionBroadcast>(e => { Amount.Value = e.TotalPrizeMoney; });
        }
    }
}