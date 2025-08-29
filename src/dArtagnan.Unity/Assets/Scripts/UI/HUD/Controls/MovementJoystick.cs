using dArtagnan.Shared;
using Game;
using Game.Player.Components;
using Game.Player.Data;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.HUD.Controls
{
    public class MovementJoystick : MonoBehaviour
    {
        private VariableJoystick _variableJoystick;
        private bool Moving => _variableJoystick.Direction != Vector2.zero;
        private Vector2 InputVectorSnapped => _variableJoystick.Direction.DirectionToInt().IntToDirection();
        private PlayerModel LocalPlayer => GameService.LocalPlayer;

        private void Awake() => _variableJoystick = GetComponent<VariableJoystick>();

        private void Update()
        {
            if (LocalPlayer == null) return;
            var newDirection = GetInputDirection();
            if (LocalPlayer.Crafting.CurrentValue)
            {
                if (newDirection != LocalPlayer.Direction.CurrentValue && newDirection != Vector2.zero)
                    PacketChannel.Raise(new UpdateItemCreatingStateFromClient { IsCreatingItem = false });
                newDirection = Vector2.zero;
            }

            if (newDirection == LocalPlayer.Direction.CurrentValue) return;
            LocalPlayer.Direction.Value = newDirection.normalized;
            PacketChannel.Raise(LocalPlayer.GetMovementDataFromClient());
        }

        private void OnDisable() => _variableJoystick.OnPointerUp(new PointerEventData(EventSystem.current));

        private Vector2 GetInputDirection()
        {
            return Moving ? InputVectorSnapped.normalized : GetKeyboardVector();
        }

        private static Vector2 GetKeyboardVector()
        {
            var direction = Vector2.zero;
            if (Input.GetKey(KeyCode.W)) direction += Vector2.up;
            if (Input.GetKey(KeyCode.S)) direction += Vector2.down;
            if (Input.GetKey(KeyCode.A)) direction += Vector2.left;
            if (Input.GetKey(KeyCode.D)) direction += Vector2.right;
            return direction.normalized;
        }
    }
}