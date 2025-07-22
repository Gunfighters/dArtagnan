using dArtagnan.Shared;
using UnityEngine;

namespace UI.HUD
{
    public class ShowToPlaying : MonoBehaviour, IChannelListener
    {
        public void Initialize()
        {
            PacketChannel.On<WaitingStartFromServer>(_ => gameObject.SetActive(false));
            PacketChannel.On<RoundStartFromServer>(_ => gameObject.SetActive(true));
        }
    }
}