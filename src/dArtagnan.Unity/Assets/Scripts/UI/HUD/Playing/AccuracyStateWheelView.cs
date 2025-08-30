using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD.Playing
{
    public class AccuracyStateWheelView : MonoBehaviour
    {
        public Image up;
        public Image keep;
        public Image down;

        private void Start()
        {
            AccuracyStateWheelPresenter.Initialize(new AccuracyStateWheelModel(), this);
        }

        public event Action<int> OnSwitch;

        public void Switch(int newState)
        {
            OnSwitch?.Invoke(newState);
        }

        public void SwitchUIOnly(Image activated)
        {
            List<Image> menuList = new() { up, keep, down };
            foreach (var menu in menuList)
            {
                var c = menu == activated ? Color.green : Color.white;
                c.a = 0.5f;
                menu.color = c;
            }
        }
    }
}