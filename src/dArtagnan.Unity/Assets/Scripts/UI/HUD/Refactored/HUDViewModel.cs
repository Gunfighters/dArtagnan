using R3;

namespace UI.HUD.Refactored
{
    public class HUDViewModel
    {
        private readonly HUDModel _model;

        public HUDViewModel(HUDModel model)
        {
            _model = model;
        }
        
        public ReadOnlyReactiveProperty<bool> Controlling => _model.controlling;
        public ReadOnlyReactiveProperty<bool> Spectating => _model.spectating;
        public ReadOnlyReactiveProperty<bool> Waiting => _model.waiting;
        public ReadOnlyReactiveProperty<bool> Playing => _model.playing;
        public ReadOnlyReactiveProperty<bool> InRound =>  _model.inRound;
        public ReadOnlyReactiveProperty<bool> IsHost => _model.isHost;
    }
}