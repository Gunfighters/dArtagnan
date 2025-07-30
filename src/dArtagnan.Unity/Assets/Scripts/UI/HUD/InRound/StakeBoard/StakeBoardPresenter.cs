using Cysharp.Threading.Tasks;
using R3;

namespace UI.HUD.InRound.StakeBoard
{
    public static class StakeBoardPresenter
    {
        public static void Initialize(StakeBoardView view)
        {
            StakeBoardModel.Amount.Subscribe(a => view.text.text = $"${a}");
            StakeBoardModel.Amount.Subscribe(_ => view.Beat().Forget());
            StakeBoardModel.Amount.Subscribe(a => view.image.sprite = view.PickIconByAmount(a));
        }
    }
}