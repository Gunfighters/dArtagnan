using System;
using dArtagnan.Shared;
using Game.Player.Data;
using R3;
using UnityEngine;
using Utils;

namespace Game.Player.Components
{
    public class PlayerPhysics : MonoBehaviour
    {
        private Rigidbody2D _rb;
        [SerializeField] private float faceChangeThreshold;
        [SerializeField] private float positionCorrectionThreshold;
        [SerializeField] private float lerpSpeed;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        public void Initialize(PlayerInfoModel model)
        {
            model.Position.Subscribe(_rb.MovePosition);
        }
    }
}