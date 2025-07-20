using UnityEngine;

public class SpectatingRibbonController : MonoBehaviour
{
    private void Awake()
    {
        LocalEventChannel.OnLocalPlayerAlive += alive => gameObject.SetActive(!alive);
        gameObject.SetActive(false);
    }
}
