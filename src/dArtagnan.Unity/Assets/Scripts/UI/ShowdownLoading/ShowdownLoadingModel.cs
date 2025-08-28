using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using ObservableCollections;
using R3;

namespace UI.ShowdownLoading
{
    public static class ShowdownLoadingModel
    {
        public static readonly ObservableDictionary<int, int> Players = new();
        public static readonly ReactiveProperty<int> Countdown = new();

        public static void Initialize()
        {
            PacketChannel.On<ShowdownStartFromServer>(e =>
            {
                Players.Clear();
                foreach (var keyValuePair in e.AccuracyPool)
                {
                    Players[keyValuePair.Key] = keyValuePair.Value;
                }

                Countdown.Value = e.Countdown;
                Count().Forget();
            });
        }

        private static async UniTask Count()
        {
            while (Countdown.Value > 0)
            {
                await UniTask.WaitForSeconds(1);
                Countdown.Value--;
            }
        }
    }
}