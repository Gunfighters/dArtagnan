using System.Collections.Generic;
using System.Linq;
using Assets.HeroEditor4D.Common.Scripts.Collections;
using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using R3;
using UnityEngine;

namespace UI.Roulette
{
    [CreateAssetMenu(fileName = "RouletteModel", menuName = "d'Artagnan/Roulette Model", order = 0)]
    public class RouletteModel : ScriptableObject
    {
        public SerializableReactiveProperty<List<RouletteItem>> pool;
        public SerializableReactiveProperty<bool> nowSpin;
        public SpriteCollection gunCollection;
        public float autoSpinDelay;

        private void OnEnable()
        {
            PacketChannel.On<YourAccuracyAndPool>(OnYourAccuracyAndPool);
        }

        private void OnYourAccuracyAndPool(YourAccuracyAndPool e)
        {
            nowSpin.Value = false;
            pool.Value = e.AccuracyPool.Select(i =>
                new RouletteItem
                {
                    icon = gunCollection.GunSpriteByAccuracy(i),
                    isTarget = i == e.YourAccuracy,
                    name = $"{i}%"
                }
            ).ToList();
            ScheduleSpin(autoSpinDelay).Forget();
        }

        private async UniTask ScheduleSpin(float delay)
        {
            await UniTask.WaitForSeconds(delay);
            if (nowSpin.Value) return;
            nowSpin.Value = true;
        }
    }
}