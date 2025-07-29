using dArtagnan.Shared;
using UnityEngine;
using Utils;

namespace Game.Player.Components
{
    public class PlayerPhysics : MonoBehaviour
    {
        private Rigidbody2D _rb;
        private ModelManager _modelManager;
        private Vector2 _direction;
        public Vector2 Position => _rb.position;
        private float _speed;
        private bool _needToCorrect;
        private float _lastServerUpdateTimestamp;
        [SerializeField] private float faceChangeThreshold;
        [SerializeField] private float positionCorrectionThreshold;
        [SerializeField] private float lerpSpeed;
        private Vector2 _lastUpdatedPosition;

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

        public void Initialize(PlayerInformation info)
        {
            transform.position = info.MovementData.Position.ToUnityVec();
            _speed = info.MovementData.Speed;
            _direction = info.MovementData.Direction.IntToDirection();
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
            _rb.MovePosition(NextPosition());
        }
        
        private Vector2 NextPosition()
        {
            if (!_needToCorrect) return _rb.position + _speed * Time.fixedDeltaTime * _direction;
            var elapsed = Time.time - _lastServerUpdateTimestamp;
            var predictedPosition = _lastUpdatedPosition + _speed * elapsed * _direction;
            var diff = Vector2.Distance(_rb.position, predictedPosition);
            _needToCorrect = diff > 0.01f;
            if (diff > positionCorrectionThreshold) return predictedPosition;
            var correctionSpeed = _speed * lerpSpeed;
            if (diff > faceChangeThreshold)
            {
                SetFaceDirection(predictedPosition - _rb.position);
            }
            var needToGo = (predictedPosition - _rb.position).normalized;
            var actualDirection = needToGo.DirectionToInt().IntToDirection();
            // TODO: 최단경로 알고리즘 이용하여 벽 피해가기.
            return Vector2.MoveTowards(_rb.position, _rb.position + actualDirection * diff, correctionSpeed * Time.fixedDeltaTime);
        }    
        
        public void UpdateRemotePlayerMovement(MovementData data)
        {
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
            _modelManager.SetDirection(direction);
        }
    }
}