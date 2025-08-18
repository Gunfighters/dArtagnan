using R3;

namespace UI.HUD
{
    public static class HUDPresenter
    {
        public static void Initialize(HUDView view, HUDModel model)
        {
            model.Controlling.Subscribe(view.controls.gameObject.SetActive);
            model.Spectating.Subscribe(view.spectating.gameObject.SetActive);
            model.Waiting.Subscribe(view.waiting.gameObject.SetActive);
            model.Playing.Subscribe(view.playing.gameObject.SetActive);
            model.InRound.Subscribe(view.inRound.gameObject.SetActive);
            model.IsHost.Subscribe(view.isHost.gameObject.SetActive);
        }
    }
}