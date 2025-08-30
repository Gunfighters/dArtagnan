using System.Collections.Generic;
using dArtagnan.Shared;
using UnityEngine;

namespace UI.AugmentationSelection.Data
{
    [CreateAssetMenu(fileName = "AugmentationCollection", menuName = "d'Artagnan/Augmentation Collection", order = 0)]
    public class AugmentationCollection : ScriptableObject
    {
        public List<Augmentation> augmentations;

        public Augmentation GetAugmentationById(AugmentId id) => augmentations.Find(x => x.data.Id == id);
    }
}