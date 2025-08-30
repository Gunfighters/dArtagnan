using Game.Player.Data;
using UnityEngine;

namespace Game.Player.Components
{
    public class PlayerPhysics : MonoBehaviour
    {
        private Rigidbody2D _rb;
        [SerializeField] private float faceChangeThreshold;
        [SerializeField] private float positionCorrectionThreshold;
        [SerializeField] private float lerpSpeed;
        private PlayerModel _model;
        private const float PositionCorrectionThreshold = 4;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        public void Initialize(PlayerModel model)
        {
            _model = model;
            transform.position = _model.Position.CurrentValue;
        }

        private void FixedUpdate()
        {
            _rb.position = NextPosition();
            _model.Position.Value = _rb.position;
        }

        /// <summary>
        /// 다음 위치를 구하는 함수.
        /// </summary>
        /// <returns>다음 틱에 이동할 위치.</returns>
        private Vector2 NextPosition()
        {
            if (_model.ForceCorrectionNeeded)
            {
                _model.ForceCorrectionNeeded = false;
                return _model.Position.Value;
            }
            if (!_model.NeedToCorrect)
                return _rb.position
                       + _model.Direction.CurrentValue * (Time.fixedDeltaTime * _model.Speed.CurrentValue); // 더는 서버에서 보내준 위치대로 보정할 수 없다면, 현재 방향을 그대로 따라간다.
            var elapsed = Time.time - _model.LastServerPositionUpdateTimestamp.CurrentValue; // 현재 시각에서 마지막으로 서버에서 위치를 보내준 시각을 빼서 지금까지 경과한 시간을 구한다.
            var predictedPosition =
                _model.PositionFromServer.CurrentValue +
                _model.Direction.CurrentValue * (elapsed * _model.Speed.CurrentValue); // 마지막으로 서버에서 보내준 위치에 '경과한 시간 x 속도 x 방향'을 더해서 예상 위치를 구한다.
            var diff = Vector2.Distance(_rb.position, predictedPosition); // 현재 위치와 예상 위치의 차이를 구한다.
            _model.NeedToCorrect = diff > 0.01f; // 차이가 0.01 이상이라면 다음 틱에도 서버에서 보내준 위치로 다가가도록 보정해야만 한다. 아니라면 더는 보정하지 않는다.
            if (diff > PositionCorrectionThreshold)
                return predictedPosition; // 허용치(threshold)보다 차이가 크다면 예상 위치를 바로 리턴한다. 이러면 다음 틱에 예상 위치로 순간이동하게 된다.
            return Vector2.MoveTowards(
                _rb.position,
                predictedPosition,
                Time.fixedDeltaTime * _model.Speed.CurrentValue
            ); // 현재 위치에서 예상 위치로 이동한다. 단, 한 틱에 움직일 수 있는 최대 거리를 초과해서는 움직일 수 없다. 
        }
    }
}