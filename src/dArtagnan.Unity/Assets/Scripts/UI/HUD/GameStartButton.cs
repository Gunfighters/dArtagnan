using dArtagnan.Shared;
using UnityEngine;
using UnityEngine.UI;

public class GameStartButton : MonoBehaviour
{
    private Button _btn;

    private void Awake()
    {
        _btn = GetComponent<Button>();
    }

    private void Start()
    {
        _btn.onClick.AddListener(() => PacketChannel.Raise(new StartGameFromClient()));
    }
}