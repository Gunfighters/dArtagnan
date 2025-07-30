using System.Collections.Generic;
using System.Linq;
using UI.AugmentationSelection.Frame;
using UnityEngine;

namespace UI.AugmentationSelection
{
    public class AugmentationSelectionView : MonoBehaviour
    {
        public List<AugmentationFrame> Frames { get; private set; }
        private void Awake()
        {
            Frames = GetComponentsInChildren<AugmentationFrame>().ToList();
            AugmentationSelectionPresenter.Initialize(this);
        }
    }
}