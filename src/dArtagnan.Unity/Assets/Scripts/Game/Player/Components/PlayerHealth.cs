using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using R3;
using UnityEngine;

namespace Game.Player.Components
{
    public class PlayerHealth : MonoBehaviour
    {
        public bool Alive { get; private set; }
        private PlayerCore _core;
        [SerializeField] private Canvas infoUICanvas;

        private void Awake()
        {
            _core = GetComponent<PlayerCore>();
        }

        public void Initialize(PlayerInformation info)
        {
            gameObject.SetActive(info.Alive);
            SetAlive(info.Alive);
        }

        public void SetAlive(bool newAlive)
        {
            Alive = newAlive;
            if (Alive)
                _core.Model.Idle();
            else
            {
                _core.Model.Die();
                ScheduleDeactivation().Forget();
            }

            infoUICanvas.gameObject.SetActive(Alive);
        }

        private async UniTask ScheduleDeactivation()
        {
            await UniTask.WaitForSeconds(2);
            gameObject.SetActive(Alive);
        }
    }
}