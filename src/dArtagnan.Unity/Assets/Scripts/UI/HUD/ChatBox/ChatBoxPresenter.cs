using Game;
using ObservableCollections;
using R3;

namespace UI.HUD.ChatBox
{
    public static class ChatBoxPresenter
    {
        public static void Initialize(ChatBoxView view)
        {
            ChatBoxModel
                .Messages
                .ObserveAdd()
                .Subscribe(e => view
                    .AddChat(PlayerGeneralManager
                        .GetPlayer(e.Value.PlayerId), e.Value.Message));
        }
    }
}