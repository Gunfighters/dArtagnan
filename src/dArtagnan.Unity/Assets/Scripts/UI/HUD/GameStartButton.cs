using dArtagnan.Shared;
using Game;
using UnityEngine;
using UnityEngine.UI;

public class GameStartButton : MonoBehaviour
{
    private void Awake()
    {
        var btn = GetComponent<Button>();
        btn.onClick.AddListener(() => PacketChannel.Raise(new StartGameFromClient()));
    }
}