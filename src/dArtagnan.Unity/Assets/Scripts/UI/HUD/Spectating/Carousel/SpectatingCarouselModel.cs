using Game;
using Game.Player.Data;
using R3;

namespace UI.HUD.Spectating.Carousel
{
    public static class SpectatingCarouselModel
    {
        public static readonly ReactiveProperty<PlayerModel> SpectateTarget = new();

        public static void Initialize()
        {
            GameService.CameraTarget.Subscribe(target => SpectateTarget.Value = target);
        }
    }
}