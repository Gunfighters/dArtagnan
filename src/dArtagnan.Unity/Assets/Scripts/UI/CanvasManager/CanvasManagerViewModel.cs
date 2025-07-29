using Game;
using R3;

namespace UI.CanvasManager
{
    public class CanvasManagerViewModel
    {
        private readonly CanvasManagerModel _model;

        public CanvasManagerViewModel(CanvasManagerModel model)
        {
            _model = model;
        }

        public ReadOnlyReactiveProperty<GameScreen> Screen => _model.screen;

        public void Show(GameScreen screen)
        {
            _model.screen.Value = screen;
        }
    }
}