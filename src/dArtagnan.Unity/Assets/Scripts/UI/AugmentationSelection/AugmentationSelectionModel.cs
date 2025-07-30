using System.Collections.Generic;
using dArtagnan.Shared;
using R3;
using UnityEditor;

namespace UI.AugmentationSelection
{
    [InitializeOnLoad]
    public static class AugmentationSelectionModel
    {
        public static readonly ReactiveProperty<List<int>> Options = new();

        static AugmentationSelectionModel()
        {
            PacketChannel.On<AugmentStartFromServer>(OnAugmentationStartFromServer);
        }

        private static void OnAugmentationStartFromServer(AugmentStartFromServer e)
        {
            Options.Value = e.AugmentOptions;
        }
    }
}