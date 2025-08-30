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
        private PlayerModel _model;
        private PlayerModel _at;

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.enabled = false;
        }

        private void Update()
        {
            if (_lineRenderer.enabled)
            {
                _lineRenderer.SetPosition(1, transform.position);
                _lineRenderer.SetPosition(0, _at.Position.CurrentValue);
            }
        }

        public void Initialize(PlayerModel model)
        {
            _model = model;
            _model.Fire.Subscribe(info => Flash(info.Target).Forget());
        }
        
        private async UniTask Flash(PlayerModel at)
        {
            _at = at;
            _lineRenderer.enabled = true;
            await UniTask.WaitForSeconds(duration);
            _lineRenderer.enabled = false;
        }
    }
}