using System.Linq;
using Game;
using R3;

namespace UI.HUD.Spectating.Carousel
{
    public static class SpectatingCarouselPresenter
    {
        public static void Initialize(SpectatingCarouselView view)
        {
            SpectatingCarouselModel
                .SpectateTarget
                .Subscribe(target =>
                {
                    if (target is not null)
                    {
                        view.colorSlot.color = target.Color;
                        view.textSlot.text = target.Nickname.CurrentValue;
                    }
                });
            view
                .leftButton
                .onClick
                .AddListener(() =>
                    GameService.CameraTarget.Value = GameService
                        .Survivors
                        .SkipWhile(s => s != SpectatingCarouselModel.SpectateTarget.Value)
                        .Skip(1)
                        .DefaultIfEmpty(GameService.Survivors.First())
                        .FirstOrDefault());
            view
                .rightButton
                .onClick
                .AddListener(() =>
                    GameService.CameraTarget.Value = GameService
                        .Survivors
                        .Reverse()
                        .SkipWhile(s => s != SpectatingCarouselModel.SpectateTarget.Value)
                        .Skip(1)
                        .DefaultIfEmpty(GameService.Survivors.Reverse().First())
                        .FirstOrDefault());
        }
    }
}