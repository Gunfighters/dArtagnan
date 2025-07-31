using System;
using UnityEngine;

namespace Game.Items
{
    [Serializable]
    public struct InGameItem
    {
        public int id;
        public string name;
        public string description;
        public Sprite icon;
    }
}