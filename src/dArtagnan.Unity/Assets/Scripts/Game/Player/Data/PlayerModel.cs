using System;
using System.Collections.Generic;
using System.Linq;
using dArtagnan.Shared;
using JetBrains.Annotations;
using ObservableCollections;
using R3;
using UnityEngine;
using Utils;

namespace Game.Player.Data
{
    public class PlayerModel
    {
        public readonly ReactiveProperty<int> ID = new();
        public readonly ReactiveProperty<string> Nickname = new();
        public readonly ReactiveProperty<int> Accuracy = new();
        public readonly ReactiveProperty<int> AccuracyState = new();
        public readonly ReactiveProperty<EnergyData> EnergyData = new();
        public readonly ReactiveProperty<int> MinEnergyToShoot = new();
        public readonly ReactiveProperty<bool> Alive = new();
        public readonly ReactiveProperty<int> Targeting = new();
        public readonly ReactiveProperty<float> Range = new();
        public readonly ReactiveProperty<Vector2> Position = new();
        public readonly ReactiveProperty<Vector2> PositionFromServer = new();
        public readonly ReactiveProperty<Vector2> Direction = new();
        public readonly ReactiveProperty<float> Speed = new();
        public readonly ReactiveProperty<int> Balance = new();
        public readonly ObservableList<int> Augments = new();
        public readonly ReactiveProperty<ItemId> CurrentItem = new();
        public readonly ReactiveProperty<bool> Crafting = new();
        public readonly ReactiveProperty<float> CraftingRemainingTime = new();
        public readonly ReactiveProperty<bool> Immune = new();
        public readonly ObservableList<int> ActiveFx = new();
        public readonly Subject<FireInfo> Fire = new();
        public readonly ReactiveProperty<bool> Highlighted = new();
        public readonly ReactiveProperty<float> LastServerPositionUpdateTimestamp = new();
        public bool NeedToCorrectPosition;
        private readonly RaycastHit2D[] _raycastHits = new RaycastHit2D[20];

        private readonly List<Color> _colorPool = new List<Color>
        {
            Color.red, Color.blue, Color.green, Color.magenta, Color.yellow, Color.black, Color.cyan,
            Color.Lerp(Color.red, Color.yellow, 0.5f)
        }.Select(c => Color.Lerp(c, Color.white, 0.5f)).ToList();

        public Color Color => _colorPool[ID.CurrentValue % 8];
        
        public PlayerModel(PlayerInformation info)
        {
            ID.Value = info.PlayerId;
            Nickname.Value = info.Nickname;
            Accuracy.Value = info.Accuracy;
            AccuracyState.Value = info.AccuracyState;
            EnergyData.Value = info.EnergyData;
            MinEnergyToShoot.Value = info.MinEnergyToShoot;
            Alive.Value = info.Alive;
            Targeting.Value = info.Targeting;
            Range.Value = info.Range;
            Position.Value = PositionFromServer.Value = info.MovementData.Position.ToUnityVec();
            Direction.Value = info.MovementData.Direction.IntToDirection().normalized;
            Speed.Value = info.MovementData.Speed;
            NeedToCorrectPosition = true;
            LastServerPositionUpdateTimestamp.Value = Time.time;
            Balance.Value = info.Balance;
            Augments.Clear();
            info.Augments.ForEach(Augments.Add);
            CurrentItem.Value = (ItemId)info.CurrentItem;
            Crafting.Value = info.IsCreatingItem;
            CraftingRemainingTime.Value = info.CreatingRemainingTime;
            Immune.Value = info.HasDamageShield;
            ActiveFx.Clear();
            info.ActiveEffects.ForEach(ActiveFx.Add);
        }

        public MovementDataFromClient GetMovementDataFromClient() => new()
        {
            Direction = Direction.CurrentValue.DirectionToInt(),
            MovementData =
            {
                Direction = Direction.CurrentValue.DirectionToInt(),
                Position = Position.CurrentValue.ToSystemVec(),
                Speed = Speed.CurrentValue
            }
        };

        private bool CanShoot(PlayerModel target)
        {
            if (Vector2.Distance(target.Position.CurrentValue, Position.CurrentValue) > Range.CurrentValue) return false;
            Physics2D.RaycastNonAlloc(Position.CurrentValue, target.Position.CurrentValue - Position.CurrentValue,
                _raycastHits, Range.CurrentValue, LayerMask.GetMask("Player", "Obstacle"));
            Array.Sort(_raycastHits, (x, y) => x.distance.CompareTo(y.distance));
            return _raycastHits[1].transform == GameService.GetPlayerView(target.ID.CurrentValue)!.transform;
        }
        
        [CanBeNull]
        public PlayerModel CalculateTarget(Vector2 aim)
        {
            var self = GameService.GetPlayerModel(ID.CurrentValue)!;
            var targetPool = GameService
                .Survivors
                .Where(target => target != self)
                .Where(CanShoot)
                .ToArray();
            if (!targetPool.Any()) return null;
            if (aim == Vector2.zero)
                return targetPool
                    .Aggregate((a, b) =>
                        Vector2.Distance(a.Position.CurrentValue, self.Position.CurrentValue)
                        < Vector2.Distance(b.Position.CurrentValue, self.Position.CurrentValue)
                            ? a
                            : b);
            return targetPool
                .Aggregate((a, b) =>
                    Vector2.Angle(aim, a.Position.CurrentValue - self.Position.CurrentValue)
                    < Vector2.Angle(aim, b.Position.CurrentValue - self.Position.CurrentValue)
                        ? a
                        : b);
        }
    }
}