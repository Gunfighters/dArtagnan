using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using R3;

namespace UI.Roulette
{
    public static class RouletteModel
    {
        private const float AutoSpinDelay = 5;
        public static readonly ReactiveProperty<List<RouletteItem>> Pool = new(new List<RouletteItem>());
        public static readonly ReactiveProperty<bool> NowSpin = new();

        public static void Initialize()
        {
            PacketChannel.On<RouletteStartFromServer>(OnYourAccuracyAndPool);
        }

        private static void OnYourAccuracyAndPool(RouletteStartFromServer e)
        {
            Reset();
            Pool.Value = e.AccuracyPool.Select(i =>
                new RouletteItem
                {
                    isTarget = i == e.YourAccuracy,
                    value = i
                }
            ).ToList();
            ScheduleAutoSpin(AutoSpinDelay).Forget();
        }

        private static async UniTask ScheduleAutoSpin(float delay)
        {
            await UniTask.WaitForSeconds(delay);
            if (NowSpin.Value) return;
            NowSpin.Value = true;
        }

        public static void Reset()
        {
            NowSpin.Value = false;
        }
    }
}