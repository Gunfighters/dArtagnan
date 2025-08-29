using Game;
using Game.Player.Components;
using UnityEngine;

namespace UI.HUD.Minimap
{
    public class PlayerMinimapMarker : MonoBehaviour
    {
        public Sprite triangleSprite;
        public Sprite circleSprite;
        [SerializeField] private PlayerView view;
        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            _spriteRenderer.sprite = view.Model == GameService.LocalPlayer ? triangleSprite : circleSprite;
            _spriteRenderer.color = view.Model.Color;
            var t = Mathf.Clamp01(view.Model.Accuracy.CurrentValue / 100f);
            var scaleMultiplier = Mathf.Lerp(0.5f, 1.5f, t);
            _spriteRenderer.transform.localScale = Vector3.one * scaleMultiplier;
        }
    }
}