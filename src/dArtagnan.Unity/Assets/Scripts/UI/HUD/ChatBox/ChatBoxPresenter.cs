using Game;
using ObservableCollections;
using R3;

namespace UI.HUD.ChatBox
{
    public static class ChatBoxPresenter
    {
        public static void Initialize(ChatBoxModel model, ChatBoxView view)
        {
            model
                .Messages
                .ObserveAdd()
                .Subscribe(e =>
                {
                    if (e.Value.PlayerId == -1)
                        view.AddSystemMessage(e.Value.Message);
                    else
                        view.AddChat(GameService.GetPlayer(e.Value.PlayerId), e.Value.Message);
                })
                .AddTo(view);
        }
    }
}