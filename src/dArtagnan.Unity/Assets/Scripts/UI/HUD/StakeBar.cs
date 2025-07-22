using dArtagnan.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD
{
    public class StakeBar : MonoBehaviour, IChannelListener
    {
        [SerializeField] private Slider slider;
        [SerializeField] private TextMeshProUGUI stakeText;
        public void Initialize()
        {
            PacketChannel.On<BettingDeductionBroadcast>(OnStakeUpdate);
        }

        private void OnStakeUpdate(BettingDeductionBroadcast bettingDeduction)
        {
            slider.value = bettingDeduction.TotalPrizeMoney;
        }
    }
}