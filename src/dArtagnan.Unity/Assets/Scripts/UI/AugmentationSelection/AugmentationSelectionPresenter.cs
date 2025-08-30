using ObservableCollections;
using R3;
using UnityEngine;

namespace UI.AugmentationSelection
{
    public static class AugmentationSelectionPresenter
    {
        public static void Initialize(AugmentationSelectionModel model, AugmentationSelectionView view)
        {
            model.Options.ObserveAdd().Subscribe(added =>
            {
                Debug.Log($"Adding augmentation: {added.Value}");
                view.Frames[added.Index].Setup(model, added.Value);
            });

            model.SelectedAugmentId.Subscribe(selectedId =>
            {
                foreach (var frame in view.Frames)
                {
                    frame.UpdateSelection(frame.Augmentation.data.Id == selectedId);
                }
            });

            model.IsSelectionComplete.Subscribe(isComplete =>
            {
                if (isComplete)
                {
                    var selectedId = model.SelectedAugmentId.Value;
                    foreach (var frame in view.Frames)
                    {
                        bool isSelected = frame.Augmentation.data.Id == selectedId;
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