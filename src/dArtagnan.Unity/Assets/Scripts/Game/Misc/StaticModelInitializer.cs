using UI.AlertMessage;
using UI.AugmentationSelection;
using UI.HUD.Controls.ItemCraft;
using UI.HUD.InRound.StakeBoard;
using UI.HUD.Playing;
using UI.HUD.Spectating.Carousel;
using UI.HUD.Splashes;
using UI.ShowdownLoading;
using UnityEngine;

namespace Game.Misc
{
    public class StaticModelInitializer : MonoBehaviour
    {
        private void Start()
        {
            AugmentationSelectionModel.Initialize();
            ShowdownLoadingModel.Initialize();
            AlertMessageModel.Initialize();
            ItemCraftButtonModel.Initialize();
            SplashModel.Initialize();
            SpectatingCarouselModel.Initialize();
            AccuracyStateWheelModel.Initialize();
        }
    }
}