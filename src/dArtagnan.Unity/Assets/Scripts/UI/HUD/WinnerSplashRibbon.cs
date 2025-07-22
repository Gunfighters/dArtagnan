using System;
using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using Game;
using TMPro;
using UnityEngine;

namespace UI.HUD
{
    public class WinnerSplashRibbon : MonoBehaviour
    {
        public TextMeshProUGUI text;
        public float duration;
        private void Awake()
        {
            PacketChannel.On<RoundWinnerBroadcast>(OnWinnerBroadcast);
            gameObject.SetActive(false);
        }

        private void OnWinnerBroadcast(RoundWinnerBroadcast e)
        {
            var winner = PlayerGeneralManager.GetPlayer(e.PlayerIds[0]);
            text.text = $"{winner.Nickname} HAS WON!";
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