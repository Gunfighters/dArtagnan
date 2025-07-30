using dArtagnan.Shared;
using UnityEngine;
using Utils;

namespace Game
{
    public class PlayerCorePlayManager : MonoBehaviour, IChannelListener
    {
        public void Initialize()
        {
            PacketChannel.On<PlayerMovementDataBroadcast>(OnPlayerMovementData);
            PacketChannel.On<PlayerIsTargetingBroadcast>(OnPlayerIsTargeting);
            PacketChannel.On<PlayerShootingBroadcast>(OnPlayerShoot);
            PacketChannel.On<UpdatePlayerAlive>(OnUpdatePlayerAlive);
            PacketChannel.On<PlayerAccuracyStateBroadcast>(OnAccuracyStateBroadcast);
            PacketChannel.On<PlayerBalanceUpdateBroadcast>(OnBalanceUpdate);
        }

        private static void OnPlayerMovementData(PlayerMovementDataBroadcast e)
        {
            var targetPlayer = PlayerGeneralManager.GetPlayer(e.PlayerId);
            if (targetPlayer == PlayerGeneralManager.LocalPlayerCore) return;
            
            targetPlayer!.Physics.UpdateRemotePlayerMovement(e.MovementData);
            Debug.Log($"[패킷 수신] 플레이어 {e.PlayerId} 이동 데이터");
        }
        
        private static void OnPlayerIsTargeting(PlayerIsTargetingBroadcast playerIsTargeting)
        {
            var aiming = PlayerGeneralManager.GetPlayer(playerIsTargeting.ShooterId);
            if (aiming == PlayerGeneralManager.LocalPlayerCore) return;
            aiming!.Shoot.SetTarget(PlayerGeneralManager.GetPlayer(playerIsTargeting.TargetId));
        }
        
        private static void OnPlayerShoot(PlayerShootingBroadcast e)
        {
            var shooter = PlayerGeneralManager.GetPlayer(e.ShooterId);
            var target = PlayerGeneralManager.GetPlayer(e.TargetId);
            shooter!.Shoot.Fire(target);
            shooter.Reload.UpdateRemainingReloadTime(shooter.Reload.TotalReloadTime);
            if (GameStateManager.GameState == GameState.Round)
            {
                shooter.Shoot.ShowHitOrMiss(e.Hit);
            }
        }

        private static void OnUpdatePlayerAlive(UpdatePlayerAlive e)
        {
            var updated = PlayerGeneralManager.GetPlayer(e.PlayerId);
            updated!.Health.SetAlive(e.Alive);
            if (updated == PlayerGeneralManager.LocalPlayerCore)
            {
                LocalEventChannel.InvokeOnLocalPlayerAlive(updated.Health.Alive);
            }
        }
        
        private static void OnAccuracyStateBroadcast(PlayerAccuracyStateBroadcast e)
        {
            PlayerGeneralManager.GetPlayer(e.PlayerId)!.Accuracy.SetAccuracyState(e.AccuracyState);
        }

        private static void OnBalanceUpdate(PlayerBalanceUpdateBroadcast e)
        {
            var updated = PlayerGeneralManager.GetPlayer(e.PlayerId);
            updated!.Balance.SetBalance(e.Balance);
            if (updated == PlayerGeneralManager.LocalPlayerCore)
            {
                LocalEventChannel.InvokeOnLocalPlayerBalanceUpdate(updated.Balance.Balance);
            }
        }
    }
}