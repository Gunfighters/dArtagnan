using Cysharp.Threading.Tasks;
using Game;
using R3;
using UnityEngine;

namespace UI.AlertMessage
{
    public class AlertMessageModel
    {
        public readonly ReactiveProperty<string> Message = new();
        public readonly ReactiveProperty<Color> Color = new();
        public readonly ReactiveProperty<bool> ShowMsg = new();
        private int _messageCounter;

        public AlertMessageModel()
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