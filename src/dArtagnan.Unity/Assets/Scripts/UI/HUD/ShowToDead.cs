using UnityEngine;

namespace UI.HUD
{
    public class ShowToDead : MonoBehaviour
    {
        private void Awake()
        {
            LocalEventChannel.OnLocalPlayerAlive += alive => gameObject.SetActive(!alive);
            gameObject.SetActive(false);
        }
    }
}