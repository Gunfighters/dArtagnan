using System.Collections.Generic;
using UnityEngine;

namespace Game.Items
{
    [CreateAssetMenu(fileName = "ItemSo", menuName = "d'Artagnan/Item ScriptableObject", order = 0)]
    public class ItemSo : ScriptableObject
    {
        public List<InGameItem> items;
    }
}