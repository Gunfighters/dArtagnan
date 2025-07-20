using dArtagnan.Shared;
using Game;
using UnityEngine;

namespace UI.HUD
{
    public class ShowToHost : MonoBehaviour
    {
        private void Awake()
        {
            LocalEventChannel.OnNewHost += (_, isHost) => gameObject.SetActive(isHost);
            PacketChannel.On<WaitingStartFromServer>(_ => gameObject.SetActive(PlayerGeneralManager.LocalPlayer == PlayerGeneralManager.HostPlayer));
            gameObject.SetActive(PlayerGeneralManager.LocalPlayer == PlayerGeneralManager.HostPlayer);
        }
    }
}