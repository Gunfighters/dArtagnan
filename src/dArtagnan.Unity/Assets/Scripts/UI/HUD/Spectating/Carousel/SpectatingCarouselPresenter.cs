using System.Linq;
using Game;
using R3;

namespace UI.HUD.Spectating.Carousel
{
    public static class SpectatingCarouselPresenter
    {
        public static void Initialize(SpectatingCarouselView view)
        {
            SpectatingCarouselModel.SpectateTarget.Subscribe(target => view.nicknameSlot.text = target.Nickname);
            view
                .leftButton
                .onClick
                .AddListener(() => LocalEventChannel
                    .InvokeOnNewCameraTarget(
                        PlayerGeneralManager
                            .Survivors
                            .SkipWhile(s => s != SpectatingCarouselModel.SpectateTarget.Value)
                            .Skip(1)
                            .DefaultIfEmpty(PlayerGeneralManager.Survivors.First())
                            .FirstOrDefault()));
            view
                .rightButton
                .onClick
                .AddListener(() => LocalEventChannel
                    .InvokeOnNewCameraTarget(
                        PlayerGeneralManager
                            .Survivors
                            .SkipWhile(s => s != SpectatingCarouselModel.SpectateTarget.Value)
                            .DefaultIfEmpty(PlayerGeneralManager.Survivors.Last())
                            .LastOrDefault()));
        }
    }
}