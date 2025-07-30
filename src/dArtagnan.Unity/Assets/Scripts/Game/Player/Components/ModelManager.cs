using System;
using System.Collections.Generic;
using System.Linq;
using Assets.HeroEditor4D.Common.Scripts.CharacterScripts;
using Assets.HeroEditor4D.Common.Scripts.Collections;
using Assets.HeroEditor4D.Common.Scripts.Data;
using Assets.HeroEditor4D.Common.Scripts.Enums;
using dArtagnan.Shared;
using UnityEngine;
using Utils;

namespace Game.Player.Components
{
    public class ModelManager : MonoBehaviour
    {
        public Character4D actualModel;
        public CharacterState initialState;
        private Vector2 _direction;
        public AudioSource fireSound;
        public FirearmCollection firearmCollection;
        private readonly List<ParticleSystem> _instances = new();
        private AudioClip ShotSoundClip;
        private CharacterState currentState;
        public SpriteCollection GunCollection;
    
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            // SetTransparent();
            SetDirection(_direction == Vector2.zero ? Vector2.down : _direction);
            SetState(initialState);
            InitializeFirearmMuzzle();
        }

        public void Initialize(PlayerInformation info)
        {
            actualModel.SetExpression("Default");
            var gunSprite = GunCollection.GunSpriteByAccuracy(info.Accuracy);
            actualModel.Equip(gunSprite, GunCollection.Firearm1H.Contains(gunSprite) ? EquipmentPart.Firearm1H : EquipmentPart.Firearm2H);
            InitializeFirearmMuzzle();
        }

        public void SetHatColor(Color color)
        {
            actualModel.SetHatColor(color);
        }

        private void SetState(CharacterState state)
        {
            actualModel.SetState(state);
        }

        public void SetDirection(Vector2 newDir)
        {
            if (newDir == Vector2.zero) return;
            _direction = newDir.SnapToCardinalDirection();
            actualModel.SetDirection(_direction);
        }

        public void Walk()
        {
            SetState(CharacterState.Walk);
        }

        public void Run()
        {
            SetState(CharacterState.Run);
        }

        public void Idle()
        {
            SetState(CharacterState.Idle);
        }

        public void Fire()
        {
            actualModel.AnimationManager.Fire();
            CreateFirearmMuzzleAndPlayShotSound();
        }
    
        public void Die()
        {
            actualModel.SetState(CharacterState.Death);
        }

        private FirearmParams GetFirearmParams()
        {
            if (actualModel.Parts[0].PrimaryWeapon is null)
            {
                throw new Exception($"PrimaryWeapon not set");
            }
            var firearm =
                actualModel.SpriteCollection.Firearm1H.SingleOrDefault(i =>
                    i.Sprites.Contains(actualModel.Parts[0].PrimaryWeapon))
                ?? actualModel.SpriteCollection.Firearm2H.SingleOrDefault(i =>
                    i.Sprites.Contains(actualModel.Parts[0].PrimaryWeapon));
            if (firearm is null)
            {
                throw new Exception($"Firearm sprite not found");
            }

            var fallback = firearm.Collection;
            var foundParams = firearmCollection.FirearmParams.SingleOrDefault(i => i.Name == firearm.Name)
                              ?? firearmCollection.FirearmParams.SingleOrDefault(i => i.Name == fallback)
                              ?? firearmCollection.FirearmParams.SingleOrDefault(i => i.Name == "Basic");
            if (foundParams is null)
            {
                throw new Exception($"Firearm params not found for {firearm.Name}.");
            }
            return foundParams;
        }

        private void CreateFirearmMuzzleAndPlayShotSound()
        {
            foreach (var muzzle in _instances.Where(i => i.gameObject.activeInHierarchy)) muzzle.Play(true);
            fireSound.PlayOneShot(ShotSoundClip);
        }

        private void InitializeFirearmMuzzle()
        {
            var firearmParams = GetFirearmParams();
            if (_instances.Count > 0)
            {
                _instances.ForEach(i => Destroy(i.gameObject));
                _instances.Clear();
            }

            for (var i = 0; i < 4; i++)
            {
                var anchor = actualModel.Parts[i].AnchorFireMuzzle;
                var muzzle = Instantiate(firearmParams.FireMuzzlePrefab, anchor);

                _instances.Add(muzzle);
            }
            ShotSoundClip = firearmParams.ShotSound;
        }

        public void EquipGun(ItemSprite item)
        {
            actualModel.Equip(item, EquipmentPart.Firearm1H);
        }
    }
}