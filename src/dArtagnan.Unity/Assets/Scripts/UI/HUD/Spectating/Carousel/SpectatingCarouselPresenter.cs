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
                        view.colorSlot.color = target.MyColor;
                        view.textSlot.text = target.Nickname;
                    }
                });
            view
                .leftButton
                .onClick
                .AddListener(() => LocalEventChannel
                    .InvokeOnNewCameraTarget(
                        GameService
                            .Survivors
                            .SkipWhile(s => s != SpectatingCarouselModel.SpectateTarget.Value)
                            .Skip(1)
                            .DefaultIfEmpty(GameService.Survivors.First())
                            .FirstOrDefault()));
            view
                .rightButton
                .onClick
                .AddListener(() => LocalEventChannel
                    .InvokeOnNewCameraTarget(
                        GameService
                            .Survivors
                            .Reverse()
                            .SkipWhile(s => s != SpectatingCarouselModel.SpectateTarget.Value)
                            .Skip(1)
                            .DefaultIfEmpty(GameService.Survivors.Reverse().First())
                            .FirstOrDefault()));
        }
    }
}