using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using JetBrains.Annotations;
using UnityEngine;

namespace Game.Player.Components
{
    public class PlayerTrajectory : MonoBehaviour
    {
        private LineRenderer _lineRenderer;
        [SerializeField] private float duration;
        [CanBeNull] private Transform _target;

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.enabled = false;
        }

        private void Update()
        {
            if (_target)
            {
                _lineRenderer.SetPosition(1, _target.position);
                _lineRenderer.SetPosition(0, transform.position);
            }
        }

        public void Initialize(PlayerInformation info)
        {
            _target = PlayerGeneralManager.GetPlayer(info.Targeting)?.transform;
        }

        public void Flash(Transform newTarget)
        {
            _Flash(newTarget).Forget();
        }

        private async UniTask _Flash(Transform newTarget)
        {
            _target = newTarget;
            _lineRenderer.enabled = true;
            await UniTask.WaitForSeconds(duration);
            _target = null;
            _lineRenderer.enabled = false;
        }
    }
}