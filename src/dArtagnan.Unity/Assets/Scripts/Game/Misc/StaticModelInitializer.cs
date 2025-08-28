using UI.AlertMessage;
using UI.AugmentationSelection;
using UI.HUD.Controls.ItemCraft;
using UI.ShowdownLoading;
using UnityEngine;

namespace Game.Misc
{
    public class StaticModelInitializer : MonoBehaviour
    {
        private void Awake()
        {
            AugmentationSelectionModel.Initialize();
            ShowdownLoadingModel.Initialize();
            AlertMessageModel.Initialize();
            ItemCraftButtonModel.Initialize();
        }
    }
}