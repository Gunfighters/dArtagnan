using System.Linq;
using Game;
using ObservableCollections;
using R3;
using UnityEngine;

namespace UI.ShowdownLoading
{
    public static class ShowdownLoadingPresenter
    {
        public static void Initialize(ShowdownLoadingView view)
        {
            ShowdownLoadingModel.Players.ObserveAdd().Subscribe(newPlayer =>
            {
                var instance = Object.Instantiate(
                    newPlayer.Value.Key == GameService.LocalPlayer.ID.CurrentValue
                        ? view.localPlayerFramePrefab
                        : view.enemyPlayerFramePrefab,
                    view.frameGroup);
                instance.Initialize(newPlayer.Value.Key, newPlayer.Value.Value,
                    GameService.GetPlayerModel(newPlayer.Value.Key)!.Nickname.CurrentValue);
            });
            ShowdownLoadingModel.Players.ObserveRemove().Subscribe(removedPlayer =>
            {
                Object.Destroy(view.frameGroup.GetComponentsInChildren<ShowdownLoadingFrame>()
                    .First(frame => frame.ID == removedPlayer.Value.Key).gameObject);
            });
            ShowdownLoadingModel.Countdown.Subscribe(newCount =>
                view.countdown.text = $"게임 시작까지\n{newCount.ToString()}");
        }
    }
}