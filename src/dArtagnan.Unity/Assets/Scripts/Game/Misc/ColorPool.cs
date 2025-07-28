using System.Collections.Generic;
using UnityEngine;

namespace Game.Misc
{
    [CreateAssetMenu(fileName = "ColorPool", menuName = "d'Artagnan/Color Pool", order = 0)]
    public class ColorPool : ScriptableObject
    {
        public List<Color> colors;
    }
}