using Game;
using Game.Player.Components;
using UnityEngine;

namespace UI.HUD.Minimap
{
    public class PlayerMinimapMarker : MonoBehaviour
    {
        public Sprite triangleSprite;
        public Sprite circleSprite;
        [SerializeField] private PlayerCore core;
        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            _spriteRenderer.sprite = core == GameService.LocalPlayer ? triangleSprite : circleSprite;
            _spriteRenderer.color = core.MyColor;
            var t = Mathf.Clamp01(core.Accuracy.Accuracy.Value / 100f);
            var scaleMultiplier = Mathf.Lerp(0.5f, 1.5f, t);
            _spriteRenderer.transform.localScale = Vector3.one * scaleMultiplier;
        }
    }
}