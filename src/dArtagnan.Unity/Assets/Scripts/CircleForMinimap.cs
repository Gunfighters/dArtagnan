using UnityEngine;

public class CircleForMinimap : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Player player;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        player = GetComponentInParent<Player>();
    }

    // Update is called once per frame
    void Update()
    {
        if (player != null && spriteRenderer != null)
        {
            // accuracy를 0~1로 정규화 (예: 0~100 기준)
            float t = Mathf.Clamp01(player.Accuracy / 100f);
            // accuracy가 높을수록 검정색에 가까워짐
            spriteRenderer.color = Color.Lerp(Color.white, Color.black, t);
        }
    }
}
