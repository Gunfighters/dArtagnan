using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using Game;
using R3;
using UnityEditor;
using UnityEngine;

namespace UI.HUD.Splashes
{
    public static class SplashModel
    {
        public static readonly ReactiveProperty<int> RoundIndex = new();
        public static readonly ReactiveProperty<bool> GameStart = new();
        public static readonly ReactiveProperty<bool> RoundStart = new();
        public static readonly ReactiveProperty<bool> RoundOver = new();
        public static readonly ReactiveProperty<bool> GameOver = new();
        private const float SplashDuration = 2.5f;
        public static readonly ReactiveProperty<List<string>> Winners = new(new List<string>());

        [RuntimeInitializeOnLoadMethod]
        public static void Initialize()
        {
            PacketChannel.On<RoundStartFromServer>(e =>
            {
                RoundIndex.Value = e.Round;
                Flash(RoundStart);
            });
            PacketChannel.On<RoundWinnerBroadcast>(e =>
            {
                SetWinners(e.PlayerIds);
                Flash(RoundOver);
            });
            PacketChannel.On<GameWinnerBroadcast>(e =>
            {
                SetWinners(e.PlayerIds);
                Flash(GameOver);
            });
        }

        private static void SetWinners(List<int> ids)
        {
            Winners.Value = ids.Select(PlayerGeneralManager.GetPlayer).Select(p => p.Nickname).ToList();
        }

        private static void Flash(ReactiveProperty<bool> splash)
        {
            splash.Value = true;
            ScheduleSplashRemoval(splash).Forget();
        }

        private static async UniTask ScheduleSplashRemoval(ReactiveProperty<bool> splash)
        {
            await UniTask.WaitForSeconds(SplashDuration);
            splash.Value = false;
        }
    }
}