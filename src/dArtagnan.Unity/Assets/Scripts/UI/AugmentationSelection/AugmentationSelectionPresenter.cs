using R3;

namespace UI.AugmentationSelection
{
    public static class AugmentationSelectionPresenter
    {
        public static void Initialize(AugmentationSelectionView view)
        {
            AugmentationSelectionModel.Options.Subscribe(options =>
            {
                for (var i = 0; i < options.Count; i++)
                    view.Frames[i].Setup(options[i]);
            });
        }
    }
}