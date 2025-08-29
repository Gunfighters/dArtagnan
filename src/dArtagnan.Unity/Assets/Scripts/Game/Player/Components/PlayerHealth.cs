using Cysharp.Threading.Tasks;
using Game.Player.Data;
using R3;
using UnityEngine;

namespace Game.Player.Components
{
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private Canvas infoUICanvas;
        private PlayerModel _model;

        public void Initialize(PlayerModel model)
        {
            _model = model;
            gameObject.SetActive(_model.Alive.CurrentValue);
            _model.Alive.Subscribe(SetAlive);
        }

        private void SetAlive(bool newAlive)
        {
            infoUICanvas.gameObject.SetActive(newAlive);
            if (newAlive) gameObject.SetActive(true);
            else ScheduleDeactivation().Forget();
        }

        private async UniTask ScheduleDeactivation()
        {
            await UniTask.WaitForSeconds(2);
            gameObject.SetActive(_model.Alive.CurrentValue);
        }
    }
}