using dArtagnan.Shared;
using R3;

namespace UI.HUD.InRound.StakeBoard
{
    public class StakeBoardModel
    {
        public readonly ReactiveProperty<int> Amount = new();

        public StakeBoardModel()
        {
            PacketChannel.On<BettingDeductionBroadcast>(e => { Amount.Value = e.TotalPrizeMoney; });
        }
    }
}