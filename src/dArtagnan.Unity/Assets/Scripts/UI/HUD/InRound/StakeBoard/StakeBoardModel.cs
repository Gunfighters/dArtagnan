using System.Collections.Generic;
using System.Linq;
using dArtagnan.Shared;
using R3;
using UnityEngine;

namespace UI.HUD.InRound.StakeBoard
{
    [CreateAssetMenu(fileName = "StakeBoard", menuName = "d'Artagnan/StakeBoard", order = 0)]
    public class StakeBoardModel : ScriptableObject
    {
        public SerializableReactiveProperty<int> amount;
        public SerializableReactiveProperty<Sprite> icon;
        public List<StakeBoardIconMeta> iconPool;

        private void OnEnable()
        {
            PacketChannel.On<RoundStartFromServer>(e =>
            {
                amount.Value = 0;
                icon.Value = PickIconByAmount(0);
            });
            PacketChannel.On<BettingDeductionBroadcast>(e =>
            {
                amount.Value = e.TotalPrizeMoney;
                icon.Value = PickIconByAmount(e.TotalPrizeMoney);
            });
        }

        private Sprite PickIconByAmount(int a)
        {
            return iconPool
                .Where(meta => meta.threshold <= a)
                .Aggregate((a, b) => a.threshold > b.threshold ? a : b)
                .icon;
        }
    }
}