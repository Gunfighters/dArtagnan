using System.Linq;
using R3;
using TMPro;

namespace UI.HUD.Splashes
{
    public static class SplashPresenter
    {
        public static void Initialize(SplashModel model, SplashView view)
        {
            model
                .RoundStart
                .Subscribe(view.roundStartSplash.SetActive);
            model
                .RoundOver
                .Subscribe(view.roundOverSplash.SetActive);
            model
                .Winners
                .Subscribe(winners =>
                {
                    var joined = winners.Any() ? string.Join(", ", winners) + "플레이어가" : "아무도";
                    var won = winners.Any() ? "승리했습니다!" : "승리하지 못했습니다!";
                    view
                        .roundOverSplash
                        .GetComponentInChildren<TextMeshProUGUI>()
                        .text = joined + won;
                });
            model
                .RoundIndex
                .Subscribe(i =>
                    view.roundStartSplash
                            .GetComponentInChildren<TextMeshProUGUI>()
                            .text = $"제{i}라운드"
                );
        }
    }
}