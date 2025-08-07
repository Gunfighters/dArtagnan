using dArtagnan.Shared;
using ObservableCollections;
using UnityEngine;

namespace UI.HUD.ChatBox
{
    public static class ChatBoxModel
    {
        public static readonly ObservableList<ChatBroadcast> Messages = new();

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            PacketChannel.On<ChatBroadcast>(Messages.Add);
        }
    }
}