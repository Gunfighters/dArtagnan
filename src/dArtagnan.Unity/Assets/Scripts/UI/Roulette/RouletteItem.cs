using System;
using Assets.HeroEditor4D.Common.Scripts.Data;
using UnityEngine;

namespace UI.Roulette
{
    [Serializable]
    public struct RouletteItem
    {
        public bool isTarget;
        public int value;
    }
}