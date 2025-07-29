using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using Game;
using TMPro;
using UnityEngine;

namespace UI.HUD
{
    public class WinnerSplashRibbon : MonoBehaviour, IChannelListener
    {
        public TextMeshProUGUI text;
        public float duration;
        public void Initialize()
        {
            PacketChannel.On<RoundWinnerBroadcast>(OnRoundWinnerBroadcast);
            PacketChannel.On<GameWinnerBroadcast>(OnGameWinnerBroadcast);
            gameObject.SetActive(false);
        }

        private void OnRoundWinnerBroadcast(RoundWinnerBroadcast e)
        {
            if (e.PlayerIds != null && e.PlayerIds.Count > 0)
            {
                List<string> winnerNames = new List<string>();
                
                foreach (int playerId in e.PlayerIds)
                {
                    var player = PlayerGeneralManager.GetPlayer(playerId);
                    if (player != null)
                    {
                        winnerNames.Add(player.Nickname);
                    }
                }
                
                text.text = $"{string.Join(", ", winnerNames)} HAS WON THE ROUND!!";
            }
            else
            {
                text.text = "NOBODY WON THE ROUND!!";
            }

            gameObject.SetActive(true);
            Disappear().Forget();
        }
        
        private void OnGameWinnerBroadcast(GameWinnerBroadcast e)
        {
            if (e.PlayerIds != null && e.PlayerIds.Count > 0)
            {
                List<string> winnerNames = new List<string>();
                
                foreach (int playerId in e.PlayerIds)
                {
                    var player = PlayerGeneralManager.GetPlayer(playerId);
                    if (player != null)
                    {
                        winnerNames.Add(player.Nickname);
                    }
                }
                
                text.text = $"{string.Join(", ", winnerNames)} HAS WON THE GAME!!";
            }
            else
            {
                text.text = "NOBODY WON THE GAME!!";
            }

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