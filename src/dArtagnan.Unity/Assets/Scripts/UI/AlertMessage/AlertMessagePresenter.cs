using R3;

namespace UI.AlertMessage
{
    public static class AlertMessagePresenter
    {
        public static void Initialize(AlertMessageModel model, AlertMessageView view)
        {
            model.Message.Subscribe(msg => view.messageText.text = msg);
            model.Color.Subscribe(color =>
            {
                view.messageText.color = color;
                view.decoImage.ForEach(deco => deco.color = color);
            });
            model.ShowMsg.Subscribe(view.gameObject.SetActive);
        }
    }
}