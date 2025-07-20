using dArtagnan.Shared;
using UnityEngine;

namespace UI.HUD
{
    public class ShowToAlive : MonoBehaviour
    {
        private void Awake()
        {
            LocalEventChannel.OnLocalPlayerAlive += gameObject.SetActive;
            PacketChannel.On<WaitingStartFromServer>(_ => gameObject.SetActive(true));
            gameObject.SetActive(false);
        }
    }
}