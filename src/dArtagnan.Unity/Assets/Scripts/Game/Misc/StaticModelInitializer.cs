using UI.AlertMessage;
using UI.AugmentationSelection;
using UI.HUD.Controls.ItemCraft;
using UI.Roulette;
using UnityEngine;

namespace Game.Misc
{
    public class StaticModelInitializer : MonoBehaviour
    {
        private void Awake()
        {
            AugmentationSelectionModel.Initialize();
            RouletteModel.Initialize();
            AlertMessageModel.Initialize();
            ItemCraftButtonModel.Initialize();
        }
    }
}