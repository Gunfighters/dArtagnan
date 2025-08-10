using R3;

namespace UI.AlertMessage
{
    public static class AlertMessagePresenter
    {
        public static void Initialize(AlertMessageView view)
        {
            AlertMessageModel.Message.Subscribe(msg => view.messageText.text = msg);
            AlertMessageModel.Color.Subscribe(color =>
            {
                view.messageText.color = color;
                view.decoImage.ForEach(deco => deco.color = color);
            });
            AlertMessageModel.ShowMsg.Subscribe(view.gameObject.SetActive);
        }
    }
}