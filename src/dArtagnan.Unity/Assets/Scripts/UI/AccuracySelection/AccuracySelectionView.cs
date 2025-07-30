using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace UI.AccuracySelection
{
    public class AccuracySelectionView : MonoBehaviour
    {
        public Image turnLabel;
        public List<AccuracySelectionOption> Options { get; private set; }

        private void Awake()
        {
            Options = GetComponentsInChildren<AccuracySelectionOption>().ToList();
            AccuracySelectionPresenter.Initialize(this);
        }
    }
}