using System;
using dArtagnan.Shared;
using Game;
using R3;
using UnityEngine;

namespace UI.HUD.Refactored
{
    [CreateAssetMenu(fileName = "HUDModel", menuName = "d'Artagnan/HUD Model", order = 0)]
    public class HUDModel : ScriptableObject
    {
        public SerializableReactiveProperty<bool> controlling;
        public SerializableReactiveProperty<bool> spectating;
        public SerializableReactiveProperty<bool> waiting;
        public SerializableReactiveProperty<bool> playing;
        public SerializableReactiveProperty<bool> inRound;
        public SerializableReactiveProperty<bool> isHost;

        private void OnEnable()
        {
            PacketChannel.On<RoundStartFromServer>(_ =>
            {
                inRound.Value = true;
                waiting.Value = false;
            });
            PacketChannel.On<WaitingStartFromServer>(_ =>
            {
                inRound.Value = false;
                waiting.Value = true;
            });
            LocalEventChannel.OnLocalPlayerAlive += value =>
            {
                controlling.Value = value;
                spectating.Value = !value;
            };
            LocalEventChannel.OnNewHost += (_, isLocalPlayerHost) =>
            {
                isHost.Value = isLocalPlayerHost;
            };
        }
    }
}