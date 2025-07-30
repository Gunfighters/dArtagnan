using System;
using UnityEngine;

namespace UI.AugmentationSelection.Data
{
    [Serializable]
    public struct Augmentation
    {
        public int id;
        public Sprite sprite;
        public string description;

        public Augmentation(int id, Sprite sprite, string description)
        {
            this.id = id;
            this.sprite = sprite;
            this.description = description;
        }
    }
}