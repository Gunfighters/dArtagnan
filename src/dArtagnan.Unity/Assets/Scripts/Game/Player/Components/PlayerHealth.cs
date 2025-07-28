using dArtagnan.Shared;
using UnityEngine;

namespace Game.Player.Components
{
    public class PlayerHealth : MonoBehaviour
    {
        public bool Alive { get; private set; }
        private ModelManager _modelManager;

        private void Awake()
        {
            _modelManager = GetComponent<ModelManager>();
        }

        public void Initialize(PlayerInformation info)
        {
            SetAlive(info.Alive);
        }

        public void SetAlive(bool newAlive)
        {
            Alive = newAlive;
            if (Alive)
                _modelManager.Idle();
            else
                _modelManager.Die();
        }
    }
}