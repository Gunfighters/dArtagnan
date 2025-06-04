using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;

public class InputInitializer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void InitializeEnhancedTouch()
    {
        EnhancedTouchSupport.Enable();
    }
}