using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

namespace Game.Player.Components
{
    public class PlayerShoot : MonoBehaviour
    {
        private ModelManager _modelManager;
        [CanBeNull] public PlayerCore Target { get; private set; }
        [SerializeField] private SpriteRenderer targetHighlightCircle;
        public float Range { get; private set; }
        private Collider2D _collider2D;
        private readonly RaycastHit2D[] _hits = new RaycastHit2D[2];
        private ContactFilter2D _contactFilter2D;
        [SerializeField] private TextMeshProUGUI hitMissText;
        [SerializeField] private Color hitTextColor;
        [SerializeField] private Color missTextColor;
        [SerializeField] private float hitMissShowingDuration;

        private void Awake()
        {
            _modelManager = GetComponent<ModelManager>();
            _collider2D = GetComponent<Collider2D>();
            _contactFilter2D.useLayerMask = true;
            _contactFilter2D.layerMask = LayerMask.GetMask("Player", "Obstacle");
            HighlightAsTarget(false);
        }

        public void Initialize(PlayerInformation info)
        {
            SetRange(info.Range);
        }

        public void SetRange(float newRange)
        {
            Range = newRange;
        }

        public void SetTarget([CanBeNull] PlayerCore target)
        {
            Target = target;
        }

        public void Aim([CanBeNull] PlayerCore target)
        {
            if (target is null)
                _modelManager.HideTrajectory();
            else
                _modelManager.ShowTrajectory(target.transform);
        }

        public void Fire(PlayerCore target)
        {
            _modelManager.SetDirection(target.transform.position - transform.position);
            _modelManager.Fire();
            _modelManager.ShowTrajectory(target.transform);
            _modelManager.ScheduleHideTrajectory().Forget();
        }
        
        public bool CanShoot(PlayerCore target)
        {
            if (Vector2.Distance(target.transform.position, transform.position) > Range) return false;
            _collider2D.Raycast(target.transform.position - transform.position, _contactFilter2D, _hits, Range);
            // hits.Sort((x, y) => x.distance.CompareTo(y.distance));
            // Debug.DrawLine(collider2D., hits[0].collider.transform.position, Color.red, 10);
            // var size = Physics2D.Raycast(Position, target.Position - Position, Range,);
            System.Array.Sort(_hits, (x, y) => x.distance.CompareTo(y.distance));
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
    }
}