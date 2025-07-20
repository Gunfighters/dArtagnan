using dArtagnan.Shared;
using UnityEngine;

namespace UI.HUD
{
    public class ShowToPlaying : MonoBehaviour
    {
        private void Awake()
        {
            PacketChannel.On<WaitingStartFromServer>(_ => gameObject.SetActive(false));
            PacketChannel.On<RoundStartFromServer>(_ => gameObject.SetActive(true));
            gameObject.SetActive(false);
        }
    }
}