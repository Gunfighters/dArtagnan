using UnityEngine;

public class TransparencyController : MonoBehaviour
{
    public float spriteAlpha;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var sprites = GetComponentsInChildren<SpriteRenderer>();
        foreach (var spriteRenderer in sprites)
        {
            var modified = spriteRenderer.color;
            modified.a = spriteAlpha;
            spriteRenderer.color = modified;
        }
    }
}
