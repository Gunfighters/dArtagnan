using System.Linq;
using R3;
using TMPro;

namespace UI.HUD.Splashes
{
    public static class SplashPresenter
    {
        public static void Initialize(SplashView view)
        {
            SplashModel
                .RoundStart
                .Subscribe(view.roundStartSplash.SetActive);
            SplashModel
                .RoundOver
                .Subscribe(view.roundOverSplash.SetActive);
            SplashModel
                .Winners
                .Subscribe(winners =>
            {
                var joined = winners.Any() ? string.Join(", ", winners) : "NOBODY";
                var haveHas = winners.Count > 1 ? " HAVE" : " HAS";
                const string won = " WON!";
                view
                    .roundOverSplash
                    .GetComponentInChildren<TextMeshProUGUI>()
                    .text = joined + haveHas + won;
            });
            SplashModel
                .RoundIndex
                .Subscribe(i =>
                    view.roundStartSplash
                        .GetComponentInChildren<TextMeshProUGUI>()
                        .text = $"ROUND #{i}"
                );
        }
    }
}