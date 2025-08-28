using dArtagnan.Shared;
using R3;
using TMPro;
using UnityEngine;

namespace UI.HUD.ChatBox
{
    public class ChatInput : MonoBehaviour
    {
        private TMP_InputField _inputField;

        private void Awake()
        {
            _inputField = GetComponent<TMP_InputField>();
            _inputField
                .onSubmit
                .AsObservable()
                .Subscribe(value => PacketChannel.Raise(new ChatFromClient { Message = value }));
            _inputField
                .onSubmit
                .AsObservable()
                .Subscribe(_ => _inputField.text = "");
            _inputField
                .onSubmit
                .AsObservable()
                .Subscribe(_ => _inputField.ActivateInputField());
        }
    }
}