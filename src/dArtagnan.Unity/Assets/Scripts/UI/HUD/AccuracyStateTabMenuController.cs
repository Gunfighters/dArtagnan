using System;
using System.Collections.Generic;
using System.Linq;
using dArtagnan.Shared;
using Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AccuracyStateTabMenuController : MonoBehaviour, IChannelListener
{
    public TextMeshProUGUI up;
    public TextMeshProUGUI keep;
    public TextMeshProUGUI down;
    public Image tabFocus;
    private int _state = 0;
    public Color highlightColor;
    public Color normalColor;

    public void Initialize()
    {
        PacketChannel.On<RoundStartFromServer>(OnGamePlaying);
        PacketChannel.On<PlayerAccuracyStateBroadcast>(OnStateBroadcast);
    }

    private void OnGamePlaying(RoundStartFromServer e)
    {
        SwitchUIOnly(e.PlayersInfo.Single(i => i.PlayerId == PlayerGeneralManager.LocalPlayer.ID).AccuracyState);
    }

    private void OnStateBroadcast(PlayerAccuracyStateBroadcast e)
    {
        if (PlayerGeneralManager.LocalPlayer.ID == e.PlayerId)
            SwitchUIOnly(e.AccuracyState);
    }

    public void Switch(int newState)
    {
        SwitchUIOnly(newState);
        PlayerGeneralManager.LocalPlayer?.SetAccuracyState(newState);
        PacketChannel.Raise(new SetAccuracyState { AccuracyState = newState});
    }

    public void SwitchUIOnly(int newState)
    {
        var selected = newState switch
        {
            1 => up,
            0 => keep,
            -1 => down,
            _ => throw new Exception($"Invalid state: {_state}")
        };
        tabFocus.transform.SetParent(selected.transform);
        var pos = tabFocus.transform.localPosition;
        pos.x = 0;
        tabFocus.transform.localPosition = pos;
        selected.color = highlightColor;
        List<TextMeshProUGUI> menuList = new() { up, keep, down };
        foreach (var menu in menuList.Where(menu => menu != selected))
        {
            menu.color = normalColor;
        }
        _state = newState;
    }
}
