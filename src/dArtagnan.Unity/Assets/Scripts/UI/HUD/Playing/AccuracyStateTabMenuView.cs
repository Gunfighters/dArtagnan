using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD.Playing
{
    public class AccuracyStateTabMenuView : MonoBehaviour
    {
        public Transform up;
        public Transform keep;
        public Transform down;
        public Image tabFocus;
        public Color highlightColor;
        public Color normalColor;

        private void Awake()
        {
            AccuracyStateTabMenuPresenter.Initialize(this);
        }

        public event Action<int> OnSwitch;

        public void Switch(int newState)
        {
            OnSwitch?.Invoke(newState);
        }

        public void SwitchUIOnly(Transform tab)
        {
            tabFocus.transform.SetParent(tab.transform, false);
            tab.GetComponentInChildren<TextMeshProUGUI>().color = highlightColor;
            List<Transform> menuList = new() { up, keep, down };
            foreach (var menu in menuList.Where(menu => menu != tab))
            {
                menu.GetComponentInChildren<TextMeshProUGUI>().color = normalColor;
            }
        }
    }
}