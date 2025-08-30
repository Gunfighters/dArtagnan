using System.Linq;
using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using Game;
using ObservableCollections;
using R3;
using UnityEngine;

namespace UI.AugmentationSelection
{
    public class AugmentationSelectionModel
    {
        public readonly ObservableList<AugmentId> Options = new();
        public readonly ReactiveProperty<AugmentId> SelectedAugmentId = new(AugmentId.None);
        public readonly ReactiveProperty<bool> IsSelectionComplete = new();

        public AugmentationSelectionModel()
        {
            Debug.Log("Creating AugmentationSElectionModel");
            GameService.AugmentationOptionPool.ObserveAdd().Subscribe(pair => Options.Add(pair.Value));
            GameService.AugmentationOptionPool.ObserveRemove().Subscribe(pair => Options.Remove(pair.Value));
            GameService.State.Subscribe(state =>
            {
                if (state == GameState.Augment)
                {
                    SelectedAugmentId.Value = AugmentId.None;
                    IsSelectionComplete.Value = false;
                    ScheduleForcedSelection().Forget();
                }
            });
            Debug.Log("Done Creating AugmentationSElectionModel");
        }

        public void SelectAugmentation(AugmentId augmentId)
        {
            if (IsSelectionComplete.Value) return;

            SelectedAugmentId.Value = augmentId;
            IsSelectionComplete.Value = true;
            PacketChannel.Raise(new AugmentDoneFromClient { SelectedAugmentID = (int)augmentId });
        }

        private async UniTask ScheduleForcedSelection()
        {
            await UniTask.WaitForSeconds(15);
            SelectAugmentation(Options.First());
        }
    }
}