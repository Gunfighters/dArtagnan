using dArtagnan.Shared;
using Game;
using Game.Player;
using Game.Player.Components;
using UnityEngine;

public class MovementJoystick : MonoBehaviour
{
    private VariableJoystick _variableJoystick;
    private bool Moving => _variableJoystick.Direction != Vector2.zero;
    private Vector2 InputVectorSnapped => _variableJoystick.Direction.DirectionToInt().IntToDirection();
    private PlayerCore LocalPlayerCore => PlayerGeneralManager.LocalPlayerCore;
    
    private float _lastSendTime = 0f;
    private const float SEND_INTERVAL = 0.1f;

    private void Awake() => _variableJoystick = GetComponent<VariableJoystick>();

    private void Update()
    {
        if (LocalPlayerCore == null) return;
        
        var newDirection = GetInputDirection();
        var oldDirection = LocalPlayerCore.Physics.MovementData.Direction.IntToDirection();
        
        bool directionChanged = newDirection != oldDirection;
        bool isMoving = newDirection != Vector2.zero;
        bool timeToPing = Time.time - _lastSendTime >= SEND_INTERVAL;
        
        if (directionChanged)
        {
            LocalPlayerCore.Physics.SetDirection(newDirection.normalized);
            PacketChannel.Raise(LocalPlayerCore.Physics.MovementData);
            _lastSendTime = Time.time;
        }
        else if (isMoving && timeToPing)
        {
            PacketChannel.Raise(LocalPlayerCore.Physics.MovementData);
            _lastSendTime = Time.time;
        }
    }

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