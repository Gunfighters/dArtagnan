using R3;

namespace UI.CanvasManager
{
    public static class CanvasManagerPresenter
    {
        public static void Initialize(CanvasManagerView view)
        {
            CanvasManagerModel.Screen.Subscribe(screen =>
            {
                view.canvasList.ForEach(c => c.canvas.gameObject.SetActive(c.screen == screen));
            });
        }
    }
}