using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using TMPro;
using UnityEngine;

namespace Game.Player.Components
{
    public class PlayerBalance : MonoBehaviour
    {
        public int Balance { get; private set; }
        [SerializeField] private TextMeshProUGUI balanceText;

        public void Initialize(PlayerInformation info)
        {
            SetBalance(info.Balance);
        }

        public void SetBalance(int newBalance)
        {
            var gain = newBalance >= Balance;
            Balance = newBalance;
            balanceText.text = $"${Balance}";
            balanceText.color = gain ? Color.green : Color.red;
            ResetBalanceTextColor().Forget();
        }
        
        private async UniTaskVoid ResetBalanceTextColor()
        {
            await UniTask.WaitForSeconds(0.5f);
            balanceText.color = Color.white;
        }
    }
}