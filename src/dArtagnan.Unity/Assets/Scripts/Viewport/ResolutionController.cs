using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ResolutionController : MonoBehaviour
{
    public int xRatio;
    public int yRatio;

    private void Awake()
    {
        SetupCameraRect();
    }

    private void SetupCameraRect()
    {
        var cam = GetComponent<Camera>();
        var rect = cam.rect;
        var scaleHeight = (float)Screen.width / Screen.height / ((float)yRatio / xRatio);
        var scaleWidth = 1 / scaleHeight;
        if (scaleHeight < 1f)
        {
            rect.height = scaleHeight;
            rect.y = (1f - scaleHeight) / 2;
        }
        else
        {
            rect.width = scaleWidth;
            rect.x = (1f - scaleWidth) / 2;
        }

        cam.rect = rect;
    }
}
