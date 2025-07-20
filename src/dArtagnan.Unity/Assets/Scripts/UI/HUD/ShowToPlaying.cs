using dArtagnan.Shared;
using UnityEngine;

namespace UI.HUD
{
    public class ShowToPlaying : MonoBehaviour
    {
        private void Awake()
        {
            PacketChannel.On<GameInWaitingFromServer>(_ => gameObject.SetActive(false));
            PacketChannel.On<GameInPlayingFromServer>(_ => gameObject.SetActive(true));
            gameObject.SetActive(false);
        }
    }
}