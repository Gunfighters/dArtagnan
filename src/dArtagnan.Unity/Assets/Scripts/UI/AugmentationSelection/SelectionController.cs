using System.Collections.Generic;
using UI.AugmentationSelection;
using UnityEngine;

public class SelectionController : MonoBehaviour
{
    [SerializeField] private List<Augmentation> augmentations;
    public void Initialize(List<int> candidates)
    {
        for (var i = 0; i < candidates.Count; i++)
        {
            augmentations[i].Initialize(candidates[i]);
        }
    }
}
