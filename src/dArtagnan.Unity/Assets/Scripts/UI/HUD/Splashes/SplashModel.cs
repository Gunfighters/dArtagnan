using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using Game;
using R3;
using UnityEngine;

namespace UI.HUD.Splashes
{
    public class SplashModel
    {
        private const float SplashDuration = 2.5f;
        public readonly ReactiveProperty<int> RoundIndex = new();
        public readonly ReactiveProperty<bool> RoundStart = new();
        public readonly ReactiveProperty<bool> RoundOver = new();
        public readonly ReactiveProperty<bool> GameOver = new();
        public readonly ReactiveProperty<List<string>> Winners = new(new List<string>());

        public SplashModel()
        {
            GameService.Round.Subscribe(r =>
            {
                RoundIndex.Value = r;
            });
            GameService.State.Subscribe(state =>
            {
                if (state == GameState.Round)
                    Flash(RoundStart);
            });
            GameService.RoundWinners.Subscribe(w =>
            {
                SetWinners(w.PlayerIds);
                Flash(RoundOver);
            });
            GameService.GameWinners.Subscribe(w =>
            {
                SetWinners(w.PlayerIds);
                Flash(GameOver);
            });
        }

        private void SetWinners(List<int> ids)
        {
            Winners.Value = ids
                .Select(GameService.GetPlayerModel)
                .Select(p => p.Nickname.CurrentValue).ToList();
        }

        private void Flash(ReactiveProperty<bool> splash)
        {
            splash.Value = true;
            ScheduleSplashRemoval(splash).Forget();
        }

        private async UniTask ScheduleSplashRemoval(ReactiveProperty<bool> splash)
        {
            await UniTask.WaitForSeconds(SplashDuration);
            splash.Value = false;
        }
    }
}