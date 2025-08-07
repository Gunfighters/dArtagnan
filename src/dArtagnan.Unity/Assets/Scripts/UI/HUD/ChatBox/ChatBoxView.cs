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
        private readonly List<ChatLine> _chatLines = new();

        private void Awake() => ChatBoxPresenter.Initialize(this);

        public void AddChat(PlayerCore messenger, string message)
        {
            var added = Instantiate(chatPrefab, transform);
            added.SetLine($"{messenger.Nickname}: {message}");
            _chatLines.Add(added);
            UniTask.WaitForSeconds(lineDuration).ContinueWith(() => added.FadeOut(fadeOutDuration));
            if (_chatLines.Count > lineCount)
            {
                var disappearing = _chatLines[0];
                _chatLines.Remove(disappearing);
                disappearing.FadeOut(fadeOutDuration).Forget();
            }
        }
    }
}