using System.Collections.Generic;
using R3;

namespace UI.HUD.Splashes
{
    public class SplashViewModel
    {
        private readonly SplashModel _model;

        public SplashViewModel(SplashModel model)
        {
            _model = model;
        }

        public ReadOnlyReactiveProperty<int> RoundIndex => _model.roundIndex;
        public ReadOnlyReactiveProperty<bool> GameStart => _model.gameStart;
        public ReadOnlyReactiveProperty<bool> GameOver => _model.gameOver;
        public ReadOnlyReactiveProperty<bool> RoundStart => _model.roundStart;
        public ReadOnlyReactiveProperty<bool> RoundOver => _model.roundOver;
        public ReadOnlyReactiveProperty<List<string>> Winners => _model.winners;
    }
}