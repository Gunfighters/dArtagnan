using dArtagnan.Shared;
using Game.Items;
using UnityEngine;
using Utils;

namespace Game
{
    public class GameModelUpdater : MonoBehaviour
    {
        [SerializeField] private ItemSo itemCollection;

        private void Awake()
        {
            PacketChannel.On<MovementDataBroadcast>(OnPlayerMovementData);
            PacketChannel.On<PlayerIsTargetingBroadcast>(OnPlayerIsTargeting);
            PacketChannel.On<ShootingBroadcast>(OnPlayerShoot);
            PacketChannel.On<UpdatePlayerAlive>(OnUpdatePlayerAlive);
            PacketChannel.On<UpdateAccuracyStateBroadcast>(OnAccuracyStateBroadcast);
            PacketChannel.On<BalanceUpdateBroadcast>(OnBalanceUpdate);
            PacketChannel.On<UpdateCreatingStateBroadcast>(OnCreatingState);
            PacketChannel.On<UpdateAccuracyBroadcast>(OnAccuracyUpdate);
            PacketChannel.On<ItemAcquiredBroadcast>(OnItemAcquired);
            PacketChannel.On<UpdateRangeBroadcast>(OnRangeUpdate);
            PacketChannel.On<UpdateCurrentEnergyBroadcast>(OnCurrentEnergyUpdate);
            PacketChannel.On<UpdateMaxEnergyBroadcast>(OnMaxEnergyUpdate);
            PacketChannel.On<UpdateMinEnergyToShootBroadcast>(OnMinEnergyUpdate);
            PacketChannel.On<ItemUsedBroadcast>(OnItemUse);
            PacketChannel.On<UpdateSpeedBroadcast>(OnSpeedUpdate);
            PacketChannel.On<UpdateActiveEffectsBroadcast>(OnActiveFx);
        }

        private void OnPlayerMovementData(MovementDataBroadcast e)
        {
            var targetPlayer = GameService.GetPlayerModel(e.PlayerId)!;
            if (targetPlayer == GameService.LocalPlayer) return;
            targetPlayer.Position.Value = e.MovementData.Position.ToUnityVec();
            targetPlayer.Direction.Value = e.MovementData.Direction.IntToDirection().normalized * e.MovementData.Speed;
        }

        private void OnPlayerIsTargeting(PlayerIsTargetingBroadcast e)
        {
            var aiming = GameService.GetPlayerModel(e.ShooterId)!;
            if (aiming == GameService.LocalPlayer) return;
            aiming.Targeting.Value = e.TargetId;
        }

        private void OnPlayerShoot(ShootingBroadcast e)
        {
            var shooter = GameService.GetPlayerModel(e.ShooterId)!;
            shooter.Fire.OnNext(e.Hit);
            var data = shooter.EnergyData.Value;
            data.CurrentEnergy = e.ShooterCurrentEnergy;
            shooter.EnergyData.Value = data;
        }

        private void OnCurrentEnergyUpdate(UpdateCurrentEnergyBroadcast e)
        {
            var updated = GameService.GetPlayerModel(e.PlayerId)!;
            var data = updated.EnergyData.Value;
            data.CurrentEnergy = e.CurrentEnergy;
            updated.EnergyData.Value = data;
        }

        private void OnMaxEnergyUpdate(UpdateMaxEnergyBroadcast e)
        {
            var updated = GameService.GetPlayerModel(e.PlayerId)!;
            var data = updated.EnergyData.Value;
            data.MaxEnergy = e.MaxEnergy;
            updated.EnergyData.Value = data;
        }

        private void OnMinEnergyUpdate(UpdateMinEnergyToShootBroadcast e)
        {
            var updated = GameService.GetPlayerModel(e.PlayerId)!;
            updated.MinEnergyToShoot.Value = e.MinEnergyToShoot;
        }

        private void OnUpdatePlayerAlive(UpdatePlayerAlive e)
        {
            var updated = GameService.GetPlayerModel(e.PlayerId)!;
            updated.Alive.Value = e.Alive;
            if (updated == GameService.LocalPlayer)
                GameService.LocalPlayerAlive.OnNext(e.Alive);
        }

        private void OnCreatingState(UpdateCreatingStateBroadcast e)
        {
            var creating = GameService.GetPlayerModel(e.PlayerId)!;
            creating.Crafting.Value = e.IsCreatingItem;
        }

        private void OnItemAcquired(ItemAcquiredBroadcast e)
        {
            var acquiring = GameService.GetPlayerModel(e.PlayerId)!;
            acquiring.CurrentItem.Value = (ItemId) e.ItemId;
            acquiring.Crafting.Value = false;
        }

        private void OnItemUse(ItemUsedBroadcast e)
        {
            var losing = GameService.GetPlayerModel(e.PlayerId)!;
            losing.CurrentItem.Value = ItemId.None;
        }

        private void OnAccuracyStateBroadcast(UpdateAccuracyStateBroadcast e)
        {
            GameService.GetPlayerModel(e.PlayerId)!.AccuracyState.Value = e.AccuracyState;
        }

        private void OnBalanceUpdate(BalanceUpdateBroadcast e)
        {
            var updated = GameService.GetPlayerModel(e.PlayerId);
            updated!.Balance.Value = e.Balance;
        }

        private void OnAccuracyUpdate(UpdateAccuracyBroadcast e)
        {
            var updated = GameService.GetPlayerModel(e.PlayerId);
            updated!.Accuracy.Value = e.Accuracy;
        }

        private void OnRangeUpdate(UpdateRangeBroadcast e)
        {
            var updated = GameService.GetPlayerModel(e.PlayerId)!;
            updated.Range.Value = e.Range;
        }

        private void OnSpeedUpdate(UpdateSpeedBroadcast e)
        {
            var updated = GameService.GetPlayerModel(e.PlayerId)!;
            updated.Speed.Value = e.Speed;
        }

        private void OnActiveFx(UpdateActiveEffectsBroadcast e)
        {
            var updated = GameService.GetPlayerModel(e.PlayerId)!;
            updated.ActiveFx.Clear();
            updated.ActiveFx.AddRange(e.ActiveEffects);
        }
    }
}