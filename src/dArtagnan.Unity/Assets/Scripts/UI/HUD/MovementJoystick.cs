using dArtagnan.Shared;
using Game;
using UnityEngine;

public class MovementJoystick : MonoBehaviour
{
    private VariableJoystick _variableJoystick;
    private bool Moving => _variableJoystick.Direction != Vector2.zero;
    private Vector2 InputVectorSnapped => _variableJoystick.Direction.DirectionToInt().IntToDirection();
    private Player LocalPlayer => PlayerGeneralManager.LocalPlayer;

    private void Awake() => _variableJoystick = GetComponent<VariableJoystick>();

    private void Update()
    {
        var newDirection = GetInputDirection();
        if (newDirection == LocalPlayer.CurrentDirection) return;
        LocalPlayer.SetDirection(newDirection.normalized);
        PacketChannel.Raise(LocalPlayer.MovementData);
    }

    private Vector2 GetInputDirection()
    {
        return Moving ? InputVectorSnapped.normalized : GetKeyboardVector();
    }

    private static Vector2 GetKeyboardVector()
    {
        var direction = Vector2.zero;
        if (Input.GetKey(KeyCode.W))
        {
            direction += Vector2.up;
        }

        if (Input.GetKey(KeyCode.S))
        {
            direction += Vector2.down;
        }

        if (Input.GetKey(KeyCode.A))
        {
            direction += Vector2.left;
        }

        if (Input.GetKey(KeyCode.D))
        {
            direction += Vector2.right;
        }

        return direction.normalized;
    }
}