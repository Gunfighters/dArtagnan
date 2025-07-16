using UnityEngine;

public class MovementJoystick : MonoBehaviour
{
    public VariableJoystick variableJoystick;
    public bool IsMoving => variableJoystick.Direction != Vector2.zero;

    public Vector2 InputVectorSnapped => DirectionHelperClient.IntToDirection(DirectionHelperClient.DirectionToInt(variableJoystick.Direction));
}