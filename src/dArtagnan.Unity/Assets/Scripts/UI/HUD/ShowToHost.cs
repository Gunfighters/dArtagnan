using dArtagnan.Shared;
using Game;
using UnityEngine;

namespace UI.HUD
{
    public class ShowToHost : MonoBehaviour, IChannelListener
    {
        public void Initialize()
        {
            LocalEventChannel.OnNewHost += (_, isHost) => gameObject.SetActive(isHost);
            PacketChannel.On<WaitingStartFromServer>(_ => gameObject.SetActive(PlayerGeneralManager.LocalPlayerCore == PlayerGeneralManager.HostPlayerCore));
        }
    }
}