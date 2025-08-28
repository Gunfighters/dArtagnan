using System;
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
        public readonly ReactiveProperty<Vector2> Velocity = new();
        public readonly ReactiveProperty<int> Balance = new();
        public readonly ObservableList<int> Augments = new();
        public readonly ReactiveProperty<ItemId> CurrentItem = new();
        public readonly ReactiveProperty<bool> Crafting = new();
        public readonly ReactiveProperty<float> CraftingRemainingTime = new();
        public readonly ReactiveProperty<bool> Immune = new();
        public readonly ObservableList<int> ActiveFx = new();
        public readonly Subject<bool> Fire = new();
        
        private bool _needToCorrect;
        private float _lastServerUpdateTimestamp;
        private Vector2 _lastUpdatedPosition;
        private const float PositionCorrectionThreshold = 4;
        private readonly RaycastHit2D[] _raycastHits = new RaycastHit2D[20];

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
            Position.Value = info.MovementData.Position.ToUnityVec();
            Velocity.Value = info.MovementData.Direction.IntToDirection().normalized * info.MovementData.Speed;
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
        
        /// <summary>
        /// 다음 위치를 구하는 함수.
        /// </summary>
        /// <returns>다음 틱에 이동할 위치.</returns>
        public Vector2 NextPosition()
        {
            if (!_needToCorrect)
                return Position.CurrentValue
                       + Time.fixedDeltaTime * Velocity.CurrentValue; // 더는 서버에서 보내준 위치대로 보정할 수 없다면, 현재 방향을 그대로 따라간다.
            var elapsed = Time.time - _lastServerUpdateTimestamp; // 현재 시각에서 마지막으로 서버에서 위치를 보내준 시각을 빼서 지금까지 경과한 시간을 구한다.
            var predictedPosition =
                _lastUpdatedPosition +
                elapsed * Velocity.CurrentValue; // 마지막으로 서버에서 보내준 위치에 '경과한 시간 x 속도 x 방향'을 더해서 예상 위치를 구한다.
            var diff = Vector2.Distance(Position.CurrentValue, predictedPosition); // 현재 위치와 예상 위치의 차이를 구한다.
            _needToCorrect = diff > 0.01f; // 차이가 0.01 이상이라면 다음 틱에도 서버에서 보내준 위치로 다가가도록 보정해야만 한다. 아니라면 더는 보정하지 않는다.
            if (diff > PositionCorrectionThreshold)
                return predictedPosition; // 허용치(threshold)보다 차이가 크다면 예상 위치를 바로 리턴한다. 이러면 다음 틱에 예상 위치로 순간이동하게 된다.
            return Vector2.MoveTowards(
                Position.CurrentValue,
                predictedPosition,
                Time.fixedDeltaTime * Velocity.CurrentValue.magnitude
                ); // 현재 위치에서 예상 위치로 이동한다. 단, 한 틱에 움직일 수 있는 최대 거리를 초과해서는 움직일 수 없다. 
        }

        private bool CanShoot(PlayerModel target)
        {
            if (Vector2.Distance(target.Position.CurrentValue, Position.CurrentValue) > Range.CurrentValue) return false;
            var targetTransform = GameService.GetPlayer(target.ID.CurrentValue)!.transform;
            Physics2D.RaycastNonAlloc(Position.CurrentValue, target.Position.CurrentValue - Position.CurrentValue,
                _raycastHits, Range.CurrentValue, LayerMask.GetMask("Player", "Obstacle"));
            Array.Sort(_raycastHits, (x, y) => x.distance.CompareTo(y.distance));
            return _raycastHits[1].transform == targetTransform;
        }
        
        [CanBeNull]
        public PlayerModel CalculateTarget(Vector2 aim)
        {
            var self = GameService.GetPlayer(ID.CurrentValue)!;
            var targetPool = GameService
                .Survivors
                .Where(target => target.transform != self.transform)
                .Where(target => CanShoot(target.Model))
                .ToArray();
            if (!targetPool.Any()) return null;
            if (aim == Vector2.zero)
                return targetPool
                    .Aggregate((a, b) =>
                        Vector2.Distance(a.transform.position, self.transform.position)
                        < Vector2.Distance(b.transform.position, self.transform.position)
                            ? a
                            : b).Model;
            return targetPool
                .Aggregate((a, b) =>
                    Vector2.Angle(aim, a.transform.position - self.transform.position)
                    < Vector2.Angle(aim, b.transform.position - self.transform.position)
                        ? a
                        : b).Model;
        }
    }
}