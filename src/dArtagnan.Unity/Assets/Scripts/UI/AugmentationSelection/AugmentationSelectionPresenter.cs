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
            
            AugmentationSelectionModel.SelectedAugmentId.Subscribe(selectedId =>
            {
                foreach (var frame in view.Frames)
                {
                    frame.UpdateSelection(frame.Augmentation.id == selectedId);
                }
            });
            
            AugmentationSelectionModel.IsSelectionComplete.Subscribe(isComplete =>
            {
                if (isComplete)
                {
                    var selectedId = AugmentationSelectionModel.SelectedAugmentId.Value;
                    foreach (var frame in view.Frames)
                    {
                        bool isSelected = frame.Augmentation.id == selectedId;
                        frame.SetInteractable(isSelected);
                    }
                }
                else
                {
                    foreach (var frame in view.Frames)
                    {
                        frame.SetInteractable(true);
                    }
                }
            });
        }
    }
}