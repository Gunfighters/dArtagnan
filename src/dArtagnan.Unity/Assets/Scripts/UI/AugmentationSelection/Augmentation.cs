using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.AugmentationSelection
{
    public class Augmentation : MonoBehaviour
    {
        public int ID;
        public Image image;
        public TextMeshProUGUI description;

        public void Initialize(int id)
        {
            ID = id;
        }
    }
}