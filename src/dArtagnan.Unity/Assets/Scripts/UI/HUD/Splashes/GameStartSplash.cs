using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using UnityEngine;

namespace UI.HUD.Splashes
{
    public class GameStartSplash : MonoBehaviour, IChannelListener
    {
        public float duration;
        public void Initialize()
        {
            PacketChannel.On<RoundStartFromServer>(OnGameInPlaying);
        }
        
        private void OnGameInPlaying(RoundStartFromServer e)
        {
            if (e.Round != 1) return;
            gameObject.SetActive(true);
            Disappear().Forget();
        }

        private async UniTaskVoid Disappear()
        {
            await UniTask.WaitForSeconds(duration);
            gameObject.SetActive(false);
        }
    }
}