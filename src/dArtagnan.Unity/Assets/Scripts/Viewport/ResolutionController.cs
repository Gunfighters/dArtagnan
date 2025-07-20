using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ResolutionController : MonoBehaviour
{
    public int xRatio;
    public int yRatio;

    private void Awake()
    {
        SetupCameraRect();
        // TODO: UGUI로 가려질 영역 한번 더 계산해서 가려주는 게 좋겠다. 레터박스 보이는 게 운이 좋은 것.
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
