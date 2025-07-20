using dArtagnan.Shared;
using UnityEngine;

namespace UI.HUD
{
    public class ShowToWaiting : MonoBehaviour
    {
        private void Awake()
        {
            PacketChannel.On<GameInWaitingFromServer>(_ => gameObject.SetActive(true));
            PacketChannel.On<GameInPlayingFromServer>(_ => gameObject.SetActive(false));
            gameObject.SetActive(false);
        }
    }
}