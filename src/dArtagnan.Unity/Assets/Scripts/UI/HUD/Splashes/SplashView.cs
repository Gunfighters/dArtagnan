using System.Linq;
using R3;
using TMPro;
using UnityEngine;

namespace UI.HUD.Splashes
{
    public class SplashView : MonoBehaviour
    {
        private SplashViewModel _viewModel;
        
        [Header("References")]
        [SerializeField] private SplashModel model;
        
        [Header("Splashes")]
        // [SerializeField] private GameObject gameStartSplash;
        // [SerializeField] private GameObject gameOverSplash;
        [SerializeField] private GameObject roundStartSplash;
        [SerializeField] private GameObject roundOverSplash;

        private void Awake()
        {
            _viewModel = new SplashViewModel(model);
            _viewModel.RoundStart.Subscribe(roundStartSplash.SetActive);
            _viewModel.RoundOver.Subscribe(roundOverSplash.SetActive);
            // _viewModel.GameStart.Subscribe(gameStartSplash.SetActive);
            // _viewModel.GameOver.Subscribe(gameOverSplash.SetActive);
            _viewModel.Winners.Subscribe(winners =>
            {
                var joined = string.Join(", ", winners);
                var haveHas = winners.Count > 1 ? " HAVE" : " HAS";
                const string won = " WON!";
                roundOverSplash.GetComponentInChildren<TextMeshProUGUI>().text = joined + haveHas + won;
            });
            _viewModel.RoundIndex.Subscribe(index =>
            {
                roundStartSplash.GetComponentInChildren<TextMeshProUGUI>().text = $"ROUND #{index}";
            });
        }
    }
}