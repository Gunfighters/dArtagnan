using System;
using Assets.HeroEditor4D.Common.Scripts.Data;
using UnityEngine;

namespace UI.Roulette
{
    [Serializable]
    public struct RouletteItem : IEquatable<RouletteItem>
    {
        public bool isTarget;
        public ItemSprite icon;
        public string name;

        public bool Equals(RouletteItem other)
        {
            return isTarget == other.isTarget && Equals(icon, other.icon) && name == other.name;
        }

        public override bool Equals(object obj)
        {
            return obj is RouletteItem other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(isTarget, icon, name);
        }
    }
}