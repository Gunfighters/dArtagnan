using System.Collections.Generic;
using UnityEngine;

namespace Game.Player.UI.Fx
{
    public class ActiveFx : MonoBehaviour
    {
        [SerializeField] private FxIcon iconPrefab;

        private void Awake()
        {
            ClearAll();
        }

        private void ClearAll()
        {
            foreach (var icon in GetComponentsInChildren<FxIcon>())
            {
                Destroy(icon.gameObject);
            }
        }

        public void Initialize(List<int> effectIds)
        {
            ClearAll();
            effectIds.ForEach(id => Instantiate(iconPrefab, transform).Setup(id));
        }
    }
}