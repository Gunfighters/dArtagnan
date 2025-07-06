using UnityEngine;

public class MovementJoystick : MonoBehaviour
{
    private VariableJoystick variableJoystick;
    public bool IsMoving => variableJoystick.Direction != Vector2.zero;

    public Vector2 InputVectorSnapped => DirectionHelperClient.IntToDirection(DirectionHelperClient.DirectionToInt(variableJoystick.Direction));

    private void Awake()
    {
        variableJoystick = GetComponent<VariableJoystick>();
    }
}