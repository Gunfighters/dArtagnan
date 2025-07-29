using dArtagnan.Shared;
using R3;
using UnityEngine;

namespace UI.HUD
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
                playing.Value = true;
            });
            PacketChannel.On<WaitingStartFromServer>(_ =>
            {
                inRound.Value = false;
                waiting.Value = true;
                playing.Value = false;
            });
            LocalEventChannel.OnLocalPlayerAlive += alive =>
            {
                controlling.Value = alive;
                spectating.Value = !alive;
                playing.Value = alive && inRound.Value;
            };
            LocalEventChannel.OnNewHost += (_, isLocalPlayerHost) =>
            {
                isHost.Value = isLocalPlayerHost;
            };
        }
    }
}