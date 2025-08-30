using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using Game;
using ObservableCollections;
using R3;
using UnityEngine;

namespace UI.ShowdownLoading
{
    public class ShowdownLoadingModel
    {
        public readonly ObservableDictionary<int, int> Players = new();
        public readonly ReactiveProperty<int> Countdown = new();

        public ShowdownLoadingModel()
        {
            GameService.ShowdownStartData.ObserveAdd().Subscribe(e =>
            {
                Players[e.Value.Key] = e.Value.Value;
            });
            GameService.ShowdownStartData.ObserveRemove().Subscribe(e =>
            {
                Players.Remove(e.Value.Key);
            });
            GameService.State.Subscribe(state =>
            {
                if (state == GameState.Showdown)
                {
                    Countdown.Value = GameService.StateCountdown.CurrentValue;
                    Count().Forget();
                }
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