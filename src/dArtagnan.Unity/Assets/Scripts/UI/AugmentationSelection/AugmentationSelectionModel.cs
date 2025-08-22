using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using R3;

namespace UI.AugmentationSelection
{
    public static class AugmentationSelectionModel
    {
        public static readonly ReactiveProperty<List<int>> Options = new();
        public static readonly ReactiveProperty<AugmentId> SelectedAugmentId = new(AugmentId.None);
        public static readonly ReactiveProperty<bool> IsSelectionComplete = new(false);

        public static void Initialize()
        {
            PacketChannel.On<AugmentStartFromServer>(OnAugmentationStartFromServer);
        }

        private static void OnAugmentationStartFromServer(AugmentStartFromServer e)
        {
            Options.Value = e.AugmentOptions;
            SelectedAugmentId.Value = AugmentId.None;
            IsSelectionComplete.Value = false;
            ScheduleForcedSelection().Forget();
        }

        public static void SelectAugmentation(AugmentId augmentId)
        {
            if (IsSelectionComplete.Value) return;

            SelectedAugmentId.Value = augmentId;
            IsSelectionComplete.Value = true;
            PacketChannel.Raise(new AugmentDoneFromClient { SelectedAugmentID = (int)augmentId });
        }

        private static async UniTask ScheduleForcedSelection()
        {
            await UniTask.WaitForSeconds(15);
            SelectAugmentation((AugmentId)Options.Value.First());
        }
    }
}