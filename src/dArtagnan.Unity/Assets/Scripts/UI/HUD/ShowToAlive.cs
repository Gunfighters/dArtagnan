using dArtagnan.Shared;
using UnityEngine;

namespace UI.HUD
{
    public class ShowToAlive : MonoBehaviour, IChannelListener
    {
        public void Initialize()
        {
            LocalEventChannel.OnLocalPlayerAlive += gameObject.SetActive;
            PacketChannel.On<WaitingStartFromServer>(_ => gameObject.SetActive(true));
        }
    }
}