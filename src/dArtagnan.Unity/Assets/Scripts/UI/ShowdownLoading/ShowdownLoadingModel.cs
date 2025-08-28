using dArtagnan.Shared;
using ObservableCollections;

namespace UI.ShowdownLoading
{
    public static class ShowdownLoadingModel
    {
        public static readonly ObservableDictionary<int, int> Players = new();

        public static void Initialize()
        {
            PacketChannel.On<ShowdownStartFromServer>(e => { Players.Clear(); });
        }
    }
}