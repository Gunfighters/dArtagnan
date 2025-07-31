using System.Collections.Generic;
using dArtagnan.Shared;
using R3;
using UnityEngine;

namespace UI.AugmentationSelection
{
    public static class AugmentationSelectionModel
    {
        public static readonly ReactiveProperty<List<int>> Options = new();

        [RuntimeInitializeOnLoadMethod]
        public static void Initialize()
        {
            PacketChannel.On<AugmentStartFromServer>(OnAugmentationStartFromServer);
        }

        private static void OnAugmentationStartFromServer(AugmentStartFromServer e)
        {
            Options.Value = e.AugmentOptions;
        }
    }
}