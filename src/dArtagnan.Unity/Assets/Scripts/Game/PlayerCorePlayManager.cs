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
            
        }

        private void OnPlayerMovementData(MovementDataBroadcast e)
        {
            var targetPlayer = PlayerGeneralManager.GetPlayer(e.PlayerId);
            if (targetPlayer == PlayerGeneralManager.LocalPlayerCore) return;

            targetPlayer!.Physics.UpdateRemotePlayerMovement(e.MovementData);
            Debug.Log($"[패킷 수신] 플레이어 {e.PlayerId} 이동 데이터");
        }

        private void OnPlayerIsTargeting(PlayerIsTargetingBroadcast playerIsTargeting)
        {
            var aiming = PlayerGeneralManager.GetPlayer(playerIsTargeting.ShooterId);
            if (aiming == PlayerGeneralManager.LocalPlayerCore) return;
            aiming!.Shoot.SetTarget(PlayerGeneralManager.GetPlayer(playerIsTargeting.TargetId));
        }

        private void OnPlayerShoot(ShootingBroadcast e)
        {
            var shooter = PlayerGeneralManager.GetPlayer(e.ShooterId)!;
            var target = PlayerGeneralManager.GetPlayer(e.TargetId)!;
            shooter.Shoot.Fire(target);
            shooter.Energy.UpdateCurrentEnergy(e.ShooterCurrentEnergy);
            if (GameStateManager.GameState == GameState.Round)
            {
                shooter.Shoot.ShowHitOrMiss(e.Hit);
            }
        }

        private void OnCurrentEnergyUpdate(UpdateCurrentEnergyBroadcast e)
        {
            var updated = PlayerGeneralManager.GetPlayer(e.PlayerId)!;
            updated.Energy.UpdateCurrentEnergy(e.CurrentEnergy);
        }

        private void OnMaxEnergyUpdate(UpdateMaxEnergyBroadcast e)
        {
            var updated = PlayerGeneralManager.GetPlayer(e.PlayerId)!;
            updated.Energy.UpdateMaxEnergy(e.MaxEnergy);
        }

        private void OnMinEnergyUpdate(UpdateMinEnergyToShootBroadcast e)
        {
            var updated = PlayerGeneralManager.GetPlayer(e.PlayerId)!;
            updated.Energy.SetThreshold(e.MinEnergyToShoot);
        }

        private void OnUpdatePlayerAlive(UpdatePlayerAlive e)
        {
            var updated = PlayerGeneralManager.GetPlayer(e.PlayerId);
            updated!.Health.SetAlive(e.Alive);
            if (updated == PlayerGeneralManager.LocalPlayerCore)
            {
                LocalEventChannel.InvokeOnLocalPlayerAlive(updated.Health.Alive.CurrentValue);
            }
        }

        private void OnCreatingState(UpdateCreatingStateBroadcast e)
        {
            var creating = PlayerGeneralManager.GetPlayer(e.PlayerId);
            creating!.Craft.ToggleCraft(e.IsCreatingItem);
        }

        private void OnItemAcquired(ItemAcquiredBroadcast e)
        {
            var acquiring = PlayerGeneralManager.GetPlayer(e.PlayerId)!;
            var item = itemCollection.items.Find(item => item.data.Id == (ItemId)e.ItemId);
            Debug.Log($"Player #{acquiring.ID} get Item: {item.data.Name}");
            acquiring!.Craft.ToggleCraft(false);
            acquiring!.Craft.SetItem(item);
            acquiring!.Craft.ToggleItem(true);
        }

        private void OnItemUse(ItemUsedBroadcast e)
        {
            var losing = PlayerGeneralManager.GetPlayer(e.PlayerId)!;
            losing!.Craft.ToggleItem(false);
        }

        private void OnAccuracyStateBroadcast(UpdateAccuracyStateBroadcast e)
        {
            PlayerGeneralManager.GetPlayer(e.PlayerId)!.Accuracy.SetAccuracyState(e.AccuracyState);
        }

        private void OnBalanceUpdate(BalanceUpdateBroadcast e)
        {
            var updated = PlayerGeneralManager.GetPlayer(e.PlayerId);
            updated!.Balance.SetBalance(e.Balance);
            if (updated == PlayerGeneralManager.LocalPlayerCore)
            {
                LocalEventChannel.InvokeOnLocalPlayerBalanceUpdate(updated.Balance.Balance);
            }
        }

        private void OnAccuracyUpdate(UpdateAccuracyBroadcast e)
        {
            var updated = PlayerGeneralManager.GetPlayer(e.PlayerId);
            updated!.Accuracy.SetAccuracy(e.Accuracy);
        }

        private void OnRangeUpdate(UpdateRangeBroadcast e)
        {
            var updated = PlayerGeneralManager.GetPlayer(e.PlayerId);
            updated!.Shoot.SetRange(e.Range);
        }

        private void OnSpeedUpdate(UpdateSpeedBroadcast e)
        {
            var updated = PlayerGeneralManager.GetPlayer(e.PlayerId);
            updated!.Physics.SetSpeed(e.Speed);
        }
    }
}