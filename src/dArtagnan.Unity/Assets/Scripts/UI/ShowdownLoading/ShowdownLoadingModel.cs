using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using Game;
using ObservableCollections;
using R3;

namespace UI.ShowdownLoading
{
    public class ShowdownLoadingModel
    {
        public readonly ObservableDictionary<int, int> Players = new();
        public readonly ReactiveProperty<int> Countdown = new();

        public ShowdownLoadingModel()
        {
            GameService.ShowdownStartData.Subscribe(e =>
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

        private async UniTask Count()
        {
            while (Countdown.Value > 0)
            {
                await UniTask.WaitForSeconds(1);
                Countdown.Value--;
            }
        }
    }
}