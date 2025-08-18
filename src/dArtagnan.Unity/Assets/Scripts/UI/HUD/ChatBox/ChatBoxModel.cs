using dArtagnan.Shared;
using ObservableCollections;

namespace UI.HUD.ChatBox
{
    public class ChatBoxModel
    {
        public readonly ObservableList<ChatBroadcast> Messages = new();

        public ChatBoxModel()
        {
            PacketChannel.On<ChatBroadcast>(Messages.Add);
        }
    }
}