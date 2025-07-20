using System;
using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using Game;
using TMPro;
using UnityEngine;

namespace UI.HUD
{
    public class GameStartSplash : MonoBehaviour
    {
        public float duration;
        private void Awake()
        {
            PacketChannel.On<GameInPlayingFromServer>(OnGameInPlaying);
            gameObject.SetActive(false);
        }
        
        private void OnGameInPlaying(GameInPlayingFromServer e)
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