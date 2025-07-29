using System.Collections.Generic;
using UI.AugmentationSelection;
using UnityEngine;
using Augmentation = Game.Augmentation.Augmentation;

public class SelectionController : MonoBehaviour
{
    [SerializeField] private List<Augmentation> augmentations;
    public void Initialize(List<int> candidates)
    {
        for (var i = 0; i < candidates.Count; i++)
        {
            // augmentations[i].Initialize(candidates[i]);
        }
    }
}
