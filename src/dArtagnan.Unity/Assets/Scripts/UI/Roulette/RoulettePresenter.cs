using dArtagnan.Shared;
using R3;

namespace UI.Roulette
{
    public static class RoulettePresenter
    {
        public static void Initialize(RouletteView view)
        {
            RouletteModel.NowSpin.Subscribe(view.Spin);
            RouletteModel.Pool.Subscribe(view.SetupSlots);
            view.spinButton.OnClickAsObservable().Subscribe(_ => RouletteModel.NowSpin.Value = true);
            view.OnSpinDone += () =>
            {
                PacketChannel.Raise(new RouletteDoneFromClient());
                RouletteModel.Reset();
            };
        }
    }
}