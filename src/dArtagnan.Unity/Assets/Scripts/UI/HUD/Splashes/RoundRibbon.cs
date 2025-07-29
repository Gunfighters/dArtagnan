using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using Game;
using TMPro;
using UnityEngine;

namespace UI.HUD
{
    public class RoundRibbon : MonoBehaviour, IChannelListener
    {
        public TextMeshProUGUI text;
        public float duration;
        
        public void Initialize()
        {
            PacketChannel.On<RoundStartFromServer>(OnRoundStartFromServer);
        }

        private void OnRoundStartFromServer(RoundStartFromServer e)
        {
            text.text = $"ROUND #{e.Round}";
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