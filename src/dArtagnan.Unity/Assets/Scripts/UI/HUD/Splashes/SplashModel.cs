using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using Game;
using R3;
using UnityEngine;

namespace UI.HUD.Splashes
{
    [CreateAssetMenu(fileName = "SplashModel", menuName = "d'Artagnan/Splashes Model", order = 0)]
    public class SplashModel : ScriptableObject
    {
        public SerializableReactiveProperty<int> roundIndex;
        public SerializableReactiveProperty<bool> gameStart;
        public SerializableReactiveProperty<bool> roundStart;
        public SerializableReactiveProperty<bool> roundOver;
        public SerializableReactiveProperty<bool> gameOver;
        public float splashDuration;
        public SerializableReactiveProperty<List<string>> winners;

        private void OnEnable()
        {
            PacketChannel.On<RoundStartFromServer>(_ =>
            {
                Flash(roundStart);
            });
            PacketChannel.On<RoundWinnerBroadcast>(e =>
            {
                SetWinners(e.PlayerIds);
                Flash(roundOver);
            });
            PacketChannel.On<GameWinnerBroadcast>(e =>
            {
                SetWinners(e.PlayerIds);
                Flash(gameOver);
            });
        }

        private void SetWinners(List<int> ids)
        {
            winners.Value = ids.Select(PlayerGeneralManager.GetPlayer).Select(p => p.Nickname).ToList();
        }

        private void Flash(SerializableReactiveProperty<bool> splash)
        {
            splash.Value = true;
            ScheduleSplashRemoval(splash).Forget();
        }

        private async UniTask ScheduleSplashRemoval(SerializableReactiveProperty<bool> splash)
        {
            await UniTask.WaitForSeconds(splashDuration);
            splash.Value = false;
        }
    }
}