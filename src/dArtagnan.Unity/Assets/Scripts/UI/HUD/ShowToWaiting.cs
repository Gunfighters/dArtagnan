using dArtagnan.Shared;
using UnityEngine;

namespace UI.HUD
{
    public class ShowToWaiting : MonoBehaviour, IChannelListener
    {
        public void Initialize()
        {
            PacketChannel.On<WaitingStartFromServer>(_ => gameObject.SetActive(true));
            PacketChannel.On<RoundStartFromServer>(_ => gameObject.SetActive(false));
            gameObject.SetActive(false);
        }
    }
}