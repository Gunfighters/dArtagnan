using Game.Player.Data;
using R3;
using R3.Triggers;
using UnityEngine;

namespace Game.Player.Components
{
    public class PlayerPhysics : MonoBehaviour
    {
        public Rigidbody2D Rb { get; private set; }
        [SerializeField] private float faceChangeThreshold;
        [SerializeField] private float positionCorrectionThreshold;
        [SerializeField] private float lerpSpeed;

        private void Awake()
        {
            Rb = GetComponent<Rigidbody2D>();
        }

        public void Initialize(PlayerModel model)
        {
            model.Position.Subscribe(Rb.MovePosition);
        }
    }
}