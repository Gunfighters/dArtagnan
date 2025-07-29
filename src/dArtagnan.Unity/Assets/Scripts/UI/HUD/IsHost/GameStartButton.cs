using dArtagnan.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD.IsHost
{
    public class GameStartButton : MonoBehaviour
    {
        private void Awake()
        {
            var btn = GetComponent<Button>();
            btn.onClick.AddListener(() => PacketChannel.Raise(new StartGameFromClient()));
        }
    }
}