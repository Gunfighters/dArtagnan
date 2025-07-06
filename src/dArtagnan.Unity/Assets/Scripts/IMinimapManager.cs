using UnityEngine;

public interface IMinimapManager
{
    public void UpdatePosition(int PlayerId, Vector2 Position);
    public RenderTexture MinimapRenderTexture();
}