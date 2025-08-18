using dArtagnan.Shared;
using Game.Items;
using UnityEngine;

namespace Game
{
    public class PlayerCorePlayManager : MonoBehaviour
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
            var targetPlayer = GameService.GetPlayer(e.PlayerId);
            if (targetPlayer == GameService.LocalPlayer) return;

            targetPlayer!.Physics.UpdateRemotePlayerMovement(e.MovementData);
            Debug.Log($"[패킷 수신] 플레이어 {e.PlayerId} 이동 데이터");
        }

        private void OnPlayerIsTargeting(PlayerIsTargetingBroadcast playerIsTargeting)
        {
            var aiming = GameService.GetPlayer(playerIsTargeting.ShooterId);
            if (aiming == GameService.LocalPlayer) return;
            aiming!.Shoot.SetTarget(GameService.GetPlayer(playerIsTargeting.TargetId));
        }

        private void OnPlayerShoot(ShootingBroadcast e)
        {
            var shooter = GameService.GetPlayer(e.ShooterId)!;
            var target = GameService.GetPlayer(e.TargetId)!;
            shooter.Shoot.Fire(target);
            shooter.Energy.UpdateCurrentEnergy(e.ShooterCurrentEnergy);
            if (GameService.State.CurrentValue == GameState.Round)
            {
                shooter.Shoot.ShowHitOrMiss(e.Hit);
            }
        }

        private void OnCurrentEnergyUpdate(UpdateCurrentEnergyBroadcast e)
        {
            var updated = GameService.GetPlayer(e.PlayerId)!;
            updated.Energy.UpdateCurrentEnergy(e.CurrentEnergy);
        }

        private void OnMaxEnergyUpdate(UpdateMaxEnergyBroadcast e)
        {
            var updated = GameService.GetPlayer(e.PlayerId)!;
            updated.Energy.UpdateMaxEnergy(e.MaxEnergy);
        }

        private void OnMinEnergyUpdate(UpdateMinEnergyToShootBroadcast e)
        {
            var updated = GameService.GetPlayer(e.PlayerId)!;
            updated.Energy.SetThreshold(e.MinEnergyToShoot);
        }

        private void OnUpdatePlayerAlive(UpdatePlayerAlive e)
        {
            var updated = GameService.GetPlayer(e.PlayerId);
            updated!.Health.SetAlive(e.Alive);
            if (updated == GameService.LocalPlayer)
            {
                LocalEventChannel.InvokeOnLocalPlayerAlive(updated.Health.Alive.CurrentValue);
            }
        }

        private void OnCreatingState(UpdateCreatingStateBroadcast e)
        {
            var creating = GameService.GetPlayer(e.PlayerId);
            creating!.Craft.ToggleCraft(e.IsCreatingItem);
        }

        private void OnItemAcquired(ItemAcquiredBroadcast e)
        {
            var acquiring = GameService.GetPlayer(e.PlayerId)!;
            var item = itemCollection.items.Find(item => item.data.Id == (ItemId)e.ItemId);
            Debug.Log($"Player #{acquiring.ID} get Item: {item.data.Name}");
            acquiring!.Craft.ToggleCraft(false);
            acquiring!.Craft.SetItem(item);
            acquiring!.Craft.ToggleItem(true);
        }

        private void OnItemUse(ItemUsedBroadcast e)
        {
            var losing = GameService.GetPlayer(e.PlayerId)!;
            losing!.Craft.ToggleItem(false);
        }

        private void OnAccuracyStateBroadcast(UpdateAccuracyStateBroadcast e)
        {
            GameService.GetPlayer(e.PlayerId)!.Accuracy.SetAccuracyState(e.AccuracyState);
        }

        private void OnBalanceUpdate(BalanceUpdateBroadcast e)
        {
            var updated = GameService.GetPlayer(e.PlayerId);
            updated!.Balance.SetBalance(e.Balance);
            if (updated == GameService.LocalPlayer)
            {
                LocalEventChannel.InvokeOnLocalPlayerBalanceUpdate(updated.Balance.Balance);
            }
        }

        private void OnAccuracyUpdate(UpdateAccuracyBroadcast e)
        {
            var updated = GameService.GetPlayer(e.PlayerId);
            updated!.Accuracy.SetAccuracy(e.Accuracy);
        }

        private void OnRangeUpdate(UpdateRangeBroadcast e)
        {
            var updated = GameService.GetPlayer(e.PlayerId);
            updated!.Shoot.SetRange(e.Range);
        }

        private void OnSpeedUpdate(UpdateSpeedBroadcast e)
        {
            var updated = GameService.GetPlayer(e.PlayerId);
            updated!.Physics.SetSpeed(e.Speed);
        }

        private void OnActiveFx(UpdateActiveEffectsBroadcast e)
        {
            var updated = GameService.GetPlayer(e.PlayerId);
            updated!.Fx.UpdateFx(e.ActiveEffects);
        }
    }
}