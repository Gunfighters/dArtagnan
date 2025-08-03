using System;
using System.Collections.Generic;
using System.Linq;
using Assets.HeroEditor4D.Common.Scripts.CharacterScripts;
using Assets.HeroEditor4D.Common.Scripts.Collections;
using Assets.HeroEditor4D.Common.Scripts.Data;
using Assets.HeroEditor4D.Common.Scripts.Enums;
using Assets.HeroEditor4D.InventorySystem.Scripts.Data;
using dArtagnan.Shared;
using UnityEngine;
using Utils;

namespace Game.Player.Components
{
    public class PlayerModel : MonoBehaviour
    {
        public SpriteCollection spriteCollection;
        public FirearmCollection firearmCollection;
        private readonly List<ParticleSystem> _instances = new();
        private Character4D _actualModel;
        private AudioSource _audioPlayer;
        private AudioClip _shotSoundClip;
        private ItemSprite _equipped;
        private ItemSprite _shovelSprite;
        private ItemSprite _gunSprite;
        private EquipmentPart _equipmentPartType;
        [SerializeField] private SpriteRenderer gunIcon;
        [SerializeField] private IconCollection iconCollection;

        private void Awake()
        {
            _audioPlayer = GetComponent<AudioSource>();
            _actualModel = GetComponentInChildren<Character4D>();
            _shovelSprite = spriteCollection.MeleeWeapon2H.Find(item => item.Name == "Shovel");
        }

        public void Initialize(PlayerInformation info)
        {
            SetState(info.Alive ? CharacterState.Idle : CharacterState.Death);
            SetDirection(info.MovementData.Direction.IntToDirection());
            _actualModel.SetExpression(info.Alive ? "Default" : "Dead");
            _gunSprite = spriteCollection.GunSpriteByAccuracy(info.Accuracy);
            _equipmentPartType = spriteCollection.Firearm1H.Contains(_gunSprite)
                ? EquipmentPart.Firearm1H
                : EquipmentPart.Firearm2H;
            _actualModel.Equip(_gunSprite, _equipmentPartType);
            gunIcon.sprite = iconCollection.GetIcon(_gunSprite.Id);
            InitializeFirearmMuzzle();
        }

        public void SetColor(Color color)
        {
            _actualModel.SetHatColor(color);
        }

        private void SetState(CharacterState state)
        {
            _actualModel.SetState(state);
        }

        public void SetDirection(Vector2 newDir)
        {
            if (newDir == Vector2.zero) return;
            _actualModel.SetDirection(newDir.SnapToCardinalDirection());
        }

        public void Walk()
        {
            SetState(CharacterState.Walk);
        }

        public void Idle()
        {
            if (_equipped != _gunSprite)
            {
                _equipped = _gunSprite;
                _actualModel.UnEquip(EquipmentPart.MeleeWeapon2H);
                _actualModel.Equip(_equipped, _equipmentPartType);
            }

            SetState(CharacterState.Idle);
        }

        public void Fire()
        {
            if (_equipped != _gunSprite)
            {
                _equipped = _gunSprite;
                _actualModel.UnEquip(EquipmentPart.MeleeWeapon2H);
                _actualModel.Equip(_equipped, _equipmentPartType);
            }

            _actualModel.AnimationManager.Fire();
            CreateFirearmMuzzleAndPlayShotSound();
        }

        public void Die()
        {
            _actualModel.SetState(CharacterState.Death);
        }

        public void Dig()
        {
            if (_equipped != _shovelSprite)
            {
                _actualModel.UnEquip(EquipmentPart.Firearm1H);
                _actualModel.UnEquip(EquipmentPart.Firearm2H);
                _actualModel.Equip(_shovelSprite, EquipmentPart.MeleeWeapon2H);
                _equipped = _shovelSprite;
            }

            SetDirection(Vector2.down);
            _actualModel.AnimationManager.Dig();
        }

        private FirearmParams GetFirearmParams()
        {
            if (_actualModel.Parts[0].PrimaryWeapon is null)
            {
                throw new Exception($"PrimaryWeapon not set");
            }

            var firearm =
                _actualModel.SpriteCollection.Firearm1H.SingleOrDefault(i =>
                    i.Sprites.Contains(_actualModel.Parts[0].PrimaryWeapon))
                ?? _actualModel.SpriteCollection.Firearm2H.SingleOrDefault(i =>
                    i.Sprites.Contains(_actualModel.Parts[0].PrimaryWeapon));
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
            _audioPlayer.PlayOneShot(_shotSoundClip);
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
                var anchor = _actualModel.Parts[i].AnchorFireMuzzle;
                var muzzle = Instantiate(firearmParams.FireMuzzlePrefab, anchor);

                _instances.Add(muzzle);
            }

            _shotSoundClip = firearmParams.ShotSound;
        }
    }
}