using System.Linq;
using Game;
using ObservableCollections;
using R3;
using UnityEngine;

namespace UI.ShowdownLoading
{
    public static class ShowdownLoadingPresenter
    {
        public static void Initialize(ShowdownLoadingModel model, ShowdownLoadingView view)
        {
            model.Players.ObserveAdd().Subscribe(newPlayer =>
            {
                var instance = Object.Instantiate(
                    newPlayer.Value.Key == GameService.LocalPlayer.ID.CurrentValue
                        ? view.localPlayerFramePrefab
                        : view.enemyPlayerFramePrefab,
                    view.frameGroup);
                instance.Initialize(newPlayer.Value.Key, newPlayer.Value.Value,
                    GameService.GetPlayerModel(newPlayer.Value.Key)!.Nickname.CurrentValue);
            });
            model.Players.ObserveRemove().Subscribe(removedPlayer =>
            {
                Object.Destroy(view.frameGroup.GetComponentsInChildren<ShowdownLoadingFrame>()
                    .First(frame => frame.ID == removedPlayer.Value.Key).gameObject);
            });
            model.Countdown.Subscribe(newCount =>
                view.countdown.text = $"게임 시작까지\n{newCount.ToString()}");
        }
    }
}