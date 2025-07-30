using R3;

namespace UI.HUD
{
    public static class HUDPresenter
    {
        public static void Initialize(HUDView view)
        {
            HUDModel.Controlling.Subscribe(view.controls.gameObject.SetActive);
            HUDModel.Spectating.Subscribe(view.spectating.gameObject.SetActive);
            HUDModel.Waiting.Subscribe(view.waiting.gameObject.SetActive);
            HUDModel.Playing.Subscribe(view.playing.gameObject.SetActive);
            HUDModel.InRound.Subscribe(view.inRound.gameObject.SetActive);
            HUDModel.IsHost.Subscribe(view.isHost.gameObject.SetActive);
        }
    }
}