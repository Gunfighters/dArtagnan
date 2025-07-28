using Game;
using Game.Player;
using Game.Player.Components;
using UnityEngine;

public class CircleForMinimap : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private PlayerCore _playerCore;
    private Vector3 originalScale;
    public Sprite triangleSprite;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        _playerCore = GetComponentInParent<PlayerCore>();
        originalScale = transform.localScale;

        if (_playerCore == PlayerGeneralManager.LocalPlayerCore)
        {
            spriteRenderer.sprite = triangleSprite;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (_playerCore && spriteRenderer)
        {
            // 플레이어 색깔 적용
            spriteRenderer.color = _playerCore.MyColor;
            
            // accuracy에 비례해서 원의 크기 조절
            var t = Mathf.Clamp01(_playerCore.Accuracy.Accuracy / 100f);
            var scaleMultiplier = Mathf.Lerp(0.5f, 1.5f, t);
            transform.localScale = originalScale * (scaleMultiplier * 1.5f);
        }
    }
}

