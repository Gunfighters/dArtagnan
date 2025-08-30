using Game;
using Game.Player.Data;
using R3;

namespace UI.HUD.Spectating.Carousel
{
    public class SpectatingCarouselModel
    {
        public readonly ReactiveProperty<PlayerModel> SpectateTarget = new();

        public SpectatingCarouselModel()
        {
            GameService.CameraTarget.Subscribe(target => SpectateTarget.Value = target);
        }
    }
}