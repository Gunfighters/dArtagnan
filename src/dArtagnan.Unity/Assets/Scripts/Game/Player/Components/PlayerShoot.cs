using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

namespace Game.Player.Components
{
    public class PlayerShoot : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer targetHighlightCircle;
        [SerializeField] private TextMeshProUGUI hitMissText;
        [SerializeField] private Color hitTextColor;
        [SerializeField] private Color missTextColor;
        [SerializeField] private float hitMissShowingDuration;
        private readonly RaycastHit2D[] _hits = new RaycastHit2D[2];
        private Collider2D _collider2D;
        private ContactFilter2D _contactFilter2D;
        private PlayerCore _core;
        [CanBeNull] public PlayerCore Target { get; private set; }
        public float Range { get; private set; }

        private void Awake()
        {
            _core = GetComponent<PlayerCore>();
            _collider2D = GetComponent<Collider2D>();
            _contactFilter2D.useLayerMask = true;
            _contactFilter2D.layerMask = LayerMask.GetMask("Player", "Obstacle");
            HighlightAsTarget(false);
            hitMissText.enabled = false;
        }

        public void Initialize(PlayerInformation info)
        {
            SetRange(info.Range);
        }

        public void SetRange(float newRange)
        {
            Range = newRange;
        }

        public void SetTarget([CanBeNull] PlayerCore newTarget)
        {
            Target = newTarget;
        }

        public void Fire(PlayerCore target)
        {
            _core.Model.SetDirection(target.transform.position - transform.position);
            _core.Model.Fire();
            _core.Trajectory.Flash(target.transform);
        }

        private bool CanShoot(PlayerCore target)
        {
            if (Vector2.Distance(target.transform.position, transform.position) > Range) return false;
            _collider2D.Raycast(target.transform.position - transform.position, _contactFilter2D, _hits, Range);
            Array.Sort(_hits, (x, y) => x.distance.CompareTo(y.distance));
            return _hits[0].transform == target.transform;
        }

        public void HighlightAsTarget(bool show)
        {
            targetHighlightCircle.enabled = show;
        }

        public void ShowHitOrMiss(bool hit)
        {
            hitMissText.text = hit ? "HIT!" : "MISS!";
            hitMissText.color = hit ? hitTextColor : missTextColor;
            hitMissText.enabled = true;
            HideHitMissFx().Forget();
        }

        private async UniTask HideHitMissFx()
        {
            await UniTask.WaitForSeconds(1);
            hitMissText.enabled = false;
        }

        [CanBeNull]
        public PlayerCore CalculateTarget(Vector2 aim)
        {
            var targetPool = PlayerGeneralManager
                .Survivors
                .Where(target => target.transform != transform)
                .Where(CanShoot)
                .ToArray();
            if (!targetPool.Any()) return null;
            if (aim == Vector2.zero)
                return targetPool
                    .Aggregate((a, b) =>
                        Vector2.Distance(a.transform.position, transform.position)
                        < Vector2.Distance(b.transform.position, transform.position)
                            ? a
                            : b);
            return targetPool
                .Aggregate((a, b) =>
                    Vector2.Angle(aim, a.transform.position - transform.position)
                    < Vector2.Angle(aim, b.transform.position - transform.position)
                        ? a
                        : b);
        }
    }
}