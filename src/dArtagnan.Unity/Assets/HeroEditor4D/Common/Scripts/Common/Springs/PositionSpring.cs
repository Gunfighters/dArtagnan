using UnityEngine;

namespace Assets.HeroEditor4D.Common.Scripts.Common.Springs
{
    public class PositionSpring : SpringBase
    {
        public Vector3 From;
        public Vector3 To;
        public float Dumping;

        private float _amplitude = 1;
        private Vector3 _pos;

        public void Awake()
        {
            _pos = transform.localPosition;
        }

        public void Reset()
        {
            _amplitude = 1;
        }

        public override void OnEnable()
        {
            base.OnEnable();
            Reset();
        }

        public void OnDisable()
        {
            transform.localPosition = _pos;
        }

        protected override void OnUpdate()
        {
            _amplitude = Mathf.Max(0, _amplitude - Dumping * UnityEngine.Time.deltaTime);

            transform.localPosition = _pos + (From + (To - From) * Sin()) * _amplitude;

            if (_amplitude <= 0) enabled = false;
        }
    }
}