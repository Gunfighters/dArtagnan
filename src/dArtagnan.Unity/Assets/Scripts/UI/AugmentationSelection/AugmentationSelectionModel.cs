using System.Collections.Generic;
using dArtagnan.Shared;
using R3;
using UnityEngine;

namespace UI.AugmentationSelection
{
    public static class AugmentationSelectionModel
    {
        public static readonly ReactiveProperty<List<int>> Options = new();
        public static readonly ReactiveProperty<int> SelectedAugmentId = new(-1);
        public static readonly ReactiveProperty<bool> IsSelectionComplete = new(false);

        [RuntimeInitializeOnLoadMethod]
        public static void Initialize()
        {
            PacketChannel.On<AugmentStartFromServer>(OnAugmentationStartFromServer);
        }

        private static void OnAugmentationStartFromServer(AugmentStartFromServer e)
        {
            Options.Value = e.AugmentOptions;
            SelectedAugmentId.Value = -1;
            IsSelectionComplete.Value = false;
        }

        public static void SelectAugmentation(int augmentId)
        {
            if (IsSelectionComplete.Value) return;

            SelectedAugmentId.Value = augmentId;
            IsSelectionComplete.Value = true;
            PacketChannel.Raise(new AugmentDoneFromClient { SelectedAugmentID = augmentId });
        }
    }
}