using System.Collections.Generic;
using System.Linq;
using UI.AugmentationSelection.Frame;
using UnityEngine;

namespace UI.AugmentationSelection
{
    public class AugmentationSelectionView : MonoBehaviour
    {
        public List<AugmentationFrame> Frames { get; private set; }
        private void Start()
        {
            Frames = GetComponentsInChildren<AugmentationFrame>().ToList();
            AugmentationSelectionPresenter.Initialize(new AugmentationSelectionModel(), this);
        }
    }
}