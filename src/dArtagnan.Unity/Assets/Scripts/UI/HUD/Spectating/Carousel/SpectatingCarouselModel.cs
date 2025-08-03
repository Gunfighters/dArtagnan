using Game.Player.Components;
using R3;
using UnityEngine;

namespace UI.HUD.Spectating.Carousel
{
    public static class SpectatingCarouselModel
    {
        public static readonly ReactiveProperty<PlayerCore> SpectateTarget = new();

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            LocalEventChannel.OnNewCameraTarget += target => SpectateTarget.Value = target;
        }
    }
}