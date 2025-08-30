using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Player.Components;
using Game.Player.Data;
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

        private void Start()
        {
            foreach (var c in chatLineContainer.GetComponentsInChildren<ChatLine>())
            {
                Destroy(c.gameObject);
            }

            ChatBoxPresenter.Initialize(new ChatBoxModel(), this);
        }

        public void AddChat(PlayerModel messenger, string message)
        {
            var added = Instantiate(chatPrefab, chatLineContainer);
            added.SetLine($"{messenger.Nickname.CurrentValue}: {message}");
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
                disappearing.gameObject.SetActive(false);
                Destroy(disappearing.gameObject);
            }
        }

        public void AddSystemMessage(string message)
        {
            var added = Instantiate(chatPrefab, chatLineContainer);
            added.SetLine($"System: {message}");
            added.SetSystemMessage(true);
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
                disappearing.gameObject.SetActive(false);
                Destroy(disappearing.gameObject);
            }
        }
    }
}