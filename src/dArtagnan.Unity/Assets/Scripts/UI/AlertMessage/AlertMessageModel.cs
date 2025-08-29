using Cysharp.Threading.Tasks;
using Game;
using R3;
using UnityEngine;

namespace UI.AlertMessage
{
    public static class AlertMessageModel
    {
        public static readonly ReactiveProperty<string> Message = new();
        public static readonly ReactiveProperty<Color> Color = new();
        public static readonly ReactiveProperty<bool> ShowMsg = new();
        private static int _messageCounter;

        public static void Initialize()
        {
            GameService.AlertMessage.Subscribe(msg =>
            {
                Message.Value = msg;
                ShowMsg.Value = true;
                var currentCount = ++_messageCounter;
                UniTask
                    .WaitForSeconds(0.5f)
                    .ContinueWith(() => ShowMsg.Value = currentCount != _messageCounter);
            });
        }
    }
}