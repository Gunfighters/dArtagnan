using System;
using System.Collections.Generic;
using dArtagnan.Shared;
using R3;

namespace UI.Roulette
{
    public class RouletteViewModel
    {
        private readonly RouletteModel _model;

        public RouletteViewModel(RouletteModel model)
        {
            _model = model;
        }
        
        public ReadOnlyReactiveProperty<List<RouletteItem>> Pool => _model.pool;
        public ReadOnlyReactiveProperty<bool> NowSpin => _model.nowSpin;

        public void Spin()
        {
            _model.nowSpin.Value = true;
        }

        public void OnSpinDone()
        {
            PacketChannel.Raise(new RouletteDone());
        }
    }
}