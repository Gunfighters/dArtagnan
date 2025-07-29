using TMPro;
using UnityEngine;

namespace UI.HUD
{
    public class BalanceBar : MonoBehaviour, IChannelListener
    {
        public TextMeshProUGUI balanceText;

        public void Initialize()
        {
            LocalEventChannel.OnLocalPlayerBalanceUpdate += OnBalanceUpdate;
        }

        private void OnBalanceUpdate(int balance)
        {
            Debug.Log($"Balance: {balance}");
            balanceText.text = balance.ToString();
        }
    }
}