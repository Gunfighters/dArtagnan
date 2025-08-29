using dArtagnan.Shared;
using Game.Misc;
using Game.Player.Data;
using R3;
using UnityEngine;

namespace Game.Player.Components
{
    public class PlayerView : MonoBehaviour
    {
        public PlayerModel Model { get; private set; }
        public PlayerCharacter Character { get; private set; }
        public PlayerHealth Health { get; private set; }
        public PlayerPhysics Physics { get; private set; }
        public PlayerShoot Shoot { get; private set; }
        public PlayerAccuracy Accuracy { get; private set; }
        public PlayerEnergy Energy { get; private set; }
        public PlayerBalance Balance { get; private set; }
        public PlayerTrajectory Trajectory { get; private set; }
        public PlayerCraft Craft { get; private set; }
        public PlayerFx Fx { get; private set; }

        private void Awake()
        {
            Character = GetComponent<PlayerCharacter>();
            Health = GetComponent<PlayerHealth>();
            Physics = GetComponent<PlayerPhysics>();
            Shoot = GetComponent<PlayerShoot>();
            Accuracy = GetComponent<PlayerAccuracy>();
            Energy = GetComponent<PlayerEnergy>();
            Balance = GetComponent<PlayerBalance>();
            Trajectory = GetComponent<PlayerTrajectory>();
            Craft = GetComponent<PlayerCraft>();
            Fx = GetComponent<PlayerFx>();
        }

        public void Initialize(PlayerModel model)
        {
            Model = model;
            model.Nickname.Subscribe(newName => gameObject.name = newName);
            Character.Initialize(model);
            Health.Initialize(model);
            Physics.Initialize(model);
            Shoot.Initialize(model);
            Accuracy.Initialize(model);
            Energy.Initialize(model);
            Balance.Initialize(model);
            Trajectory.Initialize(model);
            Craft.Initialize(model);
            Fx.Initialize(model);
        }
    }
}