using System;
using dArtagnan.Shared;
using UnityEngine;
using Utils;

namespace Game.Player.Components
{
    public class PlayerPhysics : MonoBehaviour
    {
        private Rigidbody2D _rb;
        private Vector2 _direction;
        public Vector2 Position => _rb.position;
        private float _speed;
        private bool _needToCorrect;
        private float _lastServerUpdateTimestamp;
        [SerializeField] private float faceChangeThreshold;
        [SerializeField] private float positionCorrectionThreshold;
        [SerializeField] private float lerpSpeed;
        private Vector2 _lastUpdatedPosition;
        private PlayerCore _core;

        public MovementDataFromClient MovementData => new()
        {
            Direction = _core.Craft.Crafting ? 0 : _direction.DirectionToInt(),
            MovementData =
            {
                Direction = _core.Craft.Crafting ? 0 : _direction.DirectionToInt(),
                Position = Position.ToSystemVec(),
                Speed = _speed
            },
        };

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _core = GetComponent<PlayerCore>();
        }

        public void Initialize(PlayerInformation info)
        {
            transform.position = info.MovementData.Position.ToUnityVec();
            _speed = info.MovementData.Speed;
            _direction = info.MovementData.Direction.IntToDirection();
            
            _lastUpdatedPosition = info.MovementData.Position.ToUnityVec();
            _needToCorrect = false;
        }

        private void Update()
        {
            if (!_core.Health.Alive.CurrentValue) return;
            if (_core.Craft.Crafting) return;
            if (_direction == Vector2.zero)
            {
                _core.Model.Idle();
            }
            else
            {
                _core.Model.Walk();
                SetFaceDirection(_direction);
            }
        }

        private void FixedUpdate()
        {
            if (!_core.Health.Alive.CurrentValue) return;
            if (_core.Craft.Crafting) return;
            _rb.MovePosition(NextPosition());
        }

        /// <summary>
        /// 다음 위치를 구하는 함수.
        /// </summary>
        /// <returns>다음 틱에 이동할 위치.</returns>
        private Vector2 NextPosition()
        {
            if (!_needToCorrect)
                return _rb.position +
                       _speed * Time.fixedDeltaTime * _direction; // 더는 서버에서 보내준 위치대로 보정할 수 없다면, 현재 방향을 그대로 따라간다.
            var elapsed = Time.time - _lastServerUpdateTimestamp; // 현재 시각에서 마지막으로 서버에서 위치를 보내준 시각을 빼서 지금까지 경과한 시간을 구한다.
            var predictedPosition =
                _lastUpdatedPosition +
                _speed * elapsed * _direction; // 마지막으로 서버에서 보내준 위치에 '경과한 시간 x 속도 x 방향'을 더해서 예상 위치를 구한다.
            var diff = Vector2.Distance(_rb.position, predictedPosition); // 현재 위치와 예상 위치의 차이를 구한다.
            _needToCorrect = diff > 0.01f; // 차이가 0.01 이상이라면 다음 틱에도 서버에서 보내준 위치로 다가가도록 보정해야만 한다. 아니라면 더는 보정하지 않는다.
            if (diff > positionCorrectionThreshold)
                return predictedPosition; // 허용치(threshold)보다 차이가 크다면 예상 위치를 바로 리턴한다. 이러면 다음 틱에 예상 위치로 순간이동하게 된다.
            if (diff > faceChangeThreshold)
                SetFaceDirection(predictedPosition - _rb.position); // 보정해야 하는 거리가 꽤 멀어서 얼굴의 방향도 바꿔야 할 경우, 얼굴을 바꿔준다.
            return Vector2.MoveTowards(_rb.position, predictedPosition,
                _speed * Time.fixedDeltaTime); // 현재 위치에서 예상 위치로 이동한다. 단, 한 틱에 움직일 수 있는 최대 거리를 초과해서는 움직일 수 없다. 
        }

        public void UpdateRemotePlayerMovement(MovementData data)
        {
            _needToCorrect = true;
            _lastUpdatedPosition = data.Position.ToUnityVec();
            _lastServerUpdateTimestamp = Time.time;
            SetDirection(data.Direction.IntToDirection());
            SetSpeed(data.Speed);
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
            _core.Model.SetDirection(direction);
        }

        public void Stop()
        {
            SetDirection(Vector2.zero);
        }
    }
}