using System;
using UnityEngine;

namespace UI.AugmentationSelection.Data
{
    [Serializable]
    public struct Augmentation
    {
        public int id;
        public string name;
        public Sprite sprite;
        public string description;

        public Augmentation(int id, string name, Sprite sprite, string description)
        {
            this.id = id;
            this.name = name;
            this.sprite = sprite;
            this.description = description;
        }
    }
}