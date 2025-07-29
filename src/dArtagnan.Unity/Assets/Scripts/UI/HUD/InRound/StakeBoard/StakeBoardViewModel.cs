using R3;
using UnityEngine;

namespace UI.HUD.InRound.StakeBoard
{
    public class StakeBoardViewModel
    {
        private readonly StakeBoardModel _model;

        public StakeBoardViewModel(StakeBoardModel model)
        {
            _model = model;
        }
        
        public ReadOnlyReactiveProperty<int> Amount =>  _model.amount;
        public ReadOnlyReactiveProperty<Sprite> Icon =>  _model.icon;
    }
}