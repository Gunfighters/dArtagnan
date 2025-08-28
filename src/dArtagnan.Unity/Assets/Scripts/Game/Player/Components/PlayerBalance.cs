using Cysharp.Threading.Tasks;
using Game.Player.Data;
using R3;
using TMPro;
using UnityEngine;

namespace Game.Player.Components
{
    public class PlayerBalance : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI balanceText;

        public void Initialize(PlayerInfoModel model)
        {
            model.Balance.Pairwise().Subscribe(tuple => SetBalance(tuple.Previous, tuple.Current));
        }

        public void SetBalance(int oldBalance, int newBalance)
        {
            var gain = newBalance >= oldBalance;
            balanceText.text = newBalance.ToString();
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