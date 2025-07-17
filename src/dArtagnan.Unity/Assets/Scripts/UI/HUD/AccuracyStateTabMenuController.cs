using System;
using System.Collections.Generic;
using System.Linq;
using dArtagnan.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class AccuracyStateTabMenuController : MonoBehaviour
{
    [SerializeField] private EventChannel packetChannel;
    public TextMeshProUGUI up;
    public TextMeshProUGUI keep;
    public TextMeshProUGUI down;
    public Image tabFocus;
    private int state = 0;
    private Player LocalPlayer => GameManager.Instance.LocalPlayer;
    public Color highlightColor;
    public Color normalColor;

    private void Start()
    {
        SwitchUIOnly(0);
    }

    public void Switch(int newState)
    {
        SwitchUIOnly(newState);
        LocalPlayer?.SetAccuracyState(newState);
        packetChannel.Raise(new SetAccuracyState() { AccuracyState = newState});
    }

    public void SwitchUIOnly(int newState)
    {
        var selected = newState switch
        {
            1 => up,
            0 => keep,
            -1 => down,
            _ => throw new Exception($"Invalid state: {state}")
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
        state = newState;
    }
}
