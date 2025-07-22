using UnityEngine;

namespace UI.HUD
{
    public class ShowToDead : MonoBehaviour, IChannelListener
    {
        public void Initialize()
        {
            LocalEventChannel.OnLocalPlayerAlive += alive => gameObject.SetActive(!alive);
        }
    }
}