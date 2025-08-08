using UnityEngine;

namespace Game.Environment
{
    [RequireComponent(typeof(Collider2D))]
    public class ColliderMinimapIndicatorMatcher : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer square;
        private Collider2D _collider;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            square.size = _collider.bounds.size;
            square.transform.localPosition = _collider.offset;
        }
    }
}