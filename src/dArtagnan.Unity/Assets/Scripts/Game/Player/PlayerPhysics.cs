using System;
using dArtagnan.Shared;
using UnityEngine;
using Utils;

public class PlayerPhysics : MonoBehaviour
{
    private Rigidbody2D _rb;
    private ModelManager _modelManager;
    private Vector2 _direction;
    public Vector2 Position => _rb.position;
    private float _speed;
    
    private Vector2 _targetPosition;
    private bool _isRemotePlayer;
    
    public PlayerMovementDataFromClient MovementData =>  new()
    {
        Direction = _direction.DirectionToInt(),
        MovementData =
        {
            Direction = _direction.DirectionToInt(),
            Position = Position.ToSystemVec(),
            Speed = _speed
        },
    };

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _modelManager = GetComponent<ModelManager>();
    }

    public void Initialize(bool isRemotePlayer)
    {
        _isRemotePlayer = isRemotePlayer;
        _targetPosition = _rb.position;
        
        if (_isRemotePlayer)
        {
            _rb.isKinematic = true;
        }
        else
        {
            _rb.isKinematic = false;
        }
    }

    private void Update()
    {
        if (_direction == Vector2.zero)
        {
            _modelManager.Idle();
        }
        else
        {
            _modelManager.Run();
            SetFaceDirection(_direction);
        }
    }

    private void FixedUpdate()
    {
        if (_isRemotePlayer)
        {
            // 원격 플레이어: 플레이어 속도에 맞춰 목표 위치로 이동
            Vector2 toTarget = (_targetPosition - _rb.position);
            float distance = toTarget.magnitude;
            
            if (distance > 0.01f)
            {
                Vector2 moveDirection = toTarget.normalized;
                float moveDistance = _speed * Time.fixedDeltaTime;
                Vector2 newPosition = _rb.position + moveDirection * Mathf.Min(moveDistance, distance);
                _rb.MovePosition(newPosition);
            }
        }
        else
        {
            _rb.MovePosition(_rb.position + _speed * Time.fixedDeltaTime * _direction);
        }
    }
    
    public void UpdateRemotePlayerMovement(MovementData data)
    {
        if (!_isRemotePlayer) return;
        
        SetDirection(data.Direction.IntToDirection());
        SetSpeed(data.Speed);
        _targetPosition = data.Position.ToUnityVec();
    }

    public void SetSpeed(float speed)
    {
        _speed = speed;
    }

    public void SetDirection(Vector2 newDir)
    {
        _direction = newDir.normalized;
        SetFaceDirection(_direction);
    }
    
    private void SetFaceDirection(Vector2 direction)
    {
        _modelManager.SetDirection(direction);
    }
}