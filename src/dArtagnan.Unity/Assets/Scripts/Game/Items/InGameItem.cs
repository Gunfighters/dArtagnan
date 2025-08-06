using System;
using dArtagnan.Shared;
using UnityEngine;

namespace Game.Items
{
    [Serializable]
    public struct InGameItem
    {
        public ItemData data;
        public Sprite icon;
    }
}