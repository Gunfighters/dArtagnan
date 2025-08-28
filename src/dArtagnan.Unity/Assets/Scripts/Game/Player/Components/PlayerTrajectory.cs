using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using Game.Player.Data;
using JetBrains.Annotations;
using R3;
using R3.Triggers;
using UnityEngine;

namespace Game.Player.Components
{
    public class PlayerTrajectory : MonoBehaviour
    {
        [SerializeField] private float duration;
        private LineRenderer _lineRenderer;

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.enabled = false;
        }

        private void Update()
        {
            _lineRenderer.SetPosition(1, transform.position);
        }

        public void Initialize(PlayerInfoModel model)
        {
            this.UpdateAsObservable().Subscribe(_ =>
            {
                var found = GameService.GetPlayer(model.Targeting.CurrentValue);
                if (found)
                    _lineRenderer.SetPosition(0, found.InfoModel.Position.CurrentValue);
            });
            model.Fire.Subscribe(_ => Flash().Forget());
        }

        private async UniTask Flash()
        {
            _lineRenderer.enabled = true;
            await UniTask.WaitForSeconds(duration);
            _lineRenderer.enabled = false;
        }
    }
}