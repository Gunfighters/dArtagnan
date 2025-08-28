using Game.Player.Components;

namespace Game.Player.Data
{
    public static class PlayerPresenter
    {
        public static void Initialize(PlayerInfoModel model, PlayerCore view)
        {
            view.Initialize(model);
        }
    }
}