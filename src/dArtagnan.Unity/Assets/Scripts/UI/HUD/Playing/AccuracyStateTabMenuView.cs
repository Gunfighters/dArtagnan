using System;
using System.Collections.Generic;
using System.Linq;
using dArtagnan.Shared;
using Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD.Playing
{
    public class AccuracyStateTabMenuView : MonoBehaviour
    {
        public TextMeshProUGUI up;
        public TextMeshProUGUI keep;
        public TextMeshProUGUI down;
        public Image tabFocus;
        public Color highlightColor;
        public Color normalColor;
        public event Action<int> OnSwitch;

        private void Awake()
        {
            AccuracyStateTabMenuPresenter.Initialize(this);
        }

        public void Switch(int newState)
        {
            OnSwitch?.Invoke(newState);
        }

        public void SwitchUIOnly(TextMeshProUGUI tab)
        {
            tabFocus.transform.SetParent(tab.transform);
            var pos = tabFocus.transform.localPosition;
            pos.x = 0;
            tabFocus.transform.localPosition = pos;
            tab.color = highlightColor;
            List<TextMeshProUGUI> menuList = new() { up, keep, down };
            foreach (var menu in menuList.Where(menu => menu != tab))
            {
                menu.color = normalColor;
            }
        }
    }
}