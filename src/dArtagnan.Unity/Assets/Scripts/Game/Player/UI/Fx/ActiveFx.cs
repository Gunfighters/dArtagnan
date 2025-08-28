using System.Collections.Generic;
using ObservableCollections;
using R3;
using UnityEngine;

namespace Game.Player.UI.Fx
{
    public class ActiveFx : MonoBehaviour
    {
        [SerializeField] private FxIcon iconPrefab;

        private void ClearAll()
        {
            foreach (var icon in GetComponentsInChildren<FxIcon>())
            {
                Destroy(icon.gameObject);
            }
        }

        public void Initialize(ObservableList<int> activeFx)
        {
            activeFx.ObserveClear().Subscribe(_ => ClearAll());
            activeFx.ObserveAdd().Subscribe(id => Instantiate(iconPrefab, transform).Setup(id.Value));
        }
    }
}