using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Player.Components;
using UnityEngine;

namespace UI.HUD.ChatBox
{
    public class ChatBoxView : MonoBehaviour
    {
        [SerializeField] private ChatLine chatPrefab;
        [SerializeField] private int lineCount;
        [SerializeField] private float lineDuration;
        [SerializeField] private float fadeOutDuration;
        [SerializeField] private Transform chatLineContainer;
        private readonly List<ChatLine> _chatLines = new();

        private void Awake()
        {
            foreach (var c in chatLineContainer.GetComponentsInChildren<ChatLine>())
            {
                Destroy(c.gameObject);
            }

            ChatBoxPresenter.Initialize(this);
        }

        public void AddChat(PlayerCore messenger, string message)
        {
            var added = Instantiate(chatPrefab, chatLineContainer);
            added.SetLine($"{messenger.Nickname}: {message}");
            _chatLines.Add(added);
            UniTask.WaitForSeconds(fadeOutDuration).ContinueWith(() =>
            {
                if (_chatLines.Contains(added))
                {
                    _chatLines.Remove(added);
                    added.FadeOut(fadeOutDuration).Forget();
                }
            });
            if (_chatLines.Count > lineCount)
            {
                var disappearing = _chatLines[0];
                _chatLines.Remove(disappearing);
                Destroy(disappearing.gameObject);
            }
        }
    }
}