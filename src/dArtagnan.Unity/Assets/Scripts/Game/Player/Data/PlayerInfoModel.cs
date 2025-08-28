using dArtagnan.Shared;
using ObservableCollections;
using R3;
using UnityEngine;
using Utils;

namespace Game.Player.Data
{
    public class PlayerInfoModel
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

        public PlayerInfoModel(PlayerInformation info)
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
    }
}