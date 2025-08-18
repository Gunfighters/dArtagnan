using dArtagnan.Shared;
using Game;
using Game.Player.Components;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.HUD.Controls
{
    public class MovementJoystick : MonoBehaviour
    {
        private VariableJoystick _variableJoystick;
        private bool Moving => _variableJoystick.Direction != Vector2.zero;
        private Vector2 InputVectorSnapped => _variableJoystick.Direction.DirectionToInt().IntToDirection();
        private PlayerCore LocalPlayer => GameService.LocalPlayer;

        private void Awake() => _variableJoystick = GetComponent<VariableJoystick>();

        private void Update()
        {
            var newDirection = GetInputDirection();
            if (LocalPlayer.Craft.Crafting)
            {
                if (newDirection != LocalPlayer.Physics.MovementData.Direction.IntToDirection() &&
                    newDirection != Vector2.zero)
                {
                    PacketChannel.Raise(new UpdateItemCreatingStateFromClient { IsCreatingItem = false });
                }

                newDirection = Vector2.zero;
            }

            if (newDirection == LocalPlayer.Physics.MovementData.Direction.IntToDirection()) return;
            LocalPlayer.Physics.SetDirection(newDirection.normalized);
            PacketChannel.Raise(LocalPlayer.Physics.MovementData);
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