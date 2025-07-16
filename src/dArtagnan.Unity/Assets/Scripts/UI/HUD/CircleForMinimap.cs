using UnityEngine;

public class CircleForMinimap : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Player player;
    private Vector3 originalScale;
    public Sprite triangleSprite;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        player = GetComponentInParent<Player>();
        originalScale = transform.localScale;

        int localPlayerLayer = LayerMask.NameToLayer("LocalPlayer");
        if (player.gameObject.layer == localPlayerLayer)
        {
            spriteRenderer.sprite = triangleSprite;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (player != null && spriteRenderer != null)
        {
            // 플레이어 색깔 적용
            spriteRenderer.color = player.MyColor;
            
            // accuracy에 비례해서 원의 크기 조절
            float t = Mathf.Clamp01(player.Accuracy / 100f);
            float scaleMultiplier = Mathf.Lerp(0.5f, 1.5f, t);
            transform.localScale = originalScale * scaleMultiplier * 1.5f;
        }
    }
}

