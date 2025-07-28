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
            if (targetPlayer == PlayerGeneralManager.LocalPlayer) return;
            
            targetPlayer.Physics.UpdateRemotePlayerMovement(e.MovementData);
            Debug.Log($"[패킷 수신] 플레이어 {e.PlayerId} 이동 데이터");
        }
        
        private static void OnPlayerIsTargeting(PlayerIsTargetingBroadcast playerIsTargeting)
        {
            var aiming = PlayerGeneralManager.GetPlayer(playerIsTargeting.ShooterId);
            if (aiming == PlayerGeneralManager.LocalPlayer) return;
            aiming.Aim(playerIsTargeting.TargetId == -1 ? null : PlayerGeneralManager.GetPlayer(playerIsTargeting.TargetId));
        }
        
        private static void OnPlayerShoot(PlayerShootingBroadcast e)
        {
            var shooter = PlayerGeneralManager.GetPlayer(e.ShooterId);
            var target = PlayerGeneralManager.GetPlayer(e.TargetId);
            shooter.Fire(target);
            shooter.UpdateRemainingReloadTime(shooter.TotalReloadTime);
            if (GameStateManager.GameState == GameState.Round)
            {
                shooter.ShowHitOrMiss(e.Hit);
            }
        }

        private static void OnUpdatePlayerAlive(UpdatePlayerAlive e)
        {
            var updated = PlayerGeneralManager.GetPlayer(e.PlayerId);
            updated.SetAlive(e.Alive);
            if (updated == PlayerGeneralManager.LocalPlayer)
            {
                LocalEventChannel.InvokeOnLocalPlayerAlive(updated.Alive);
            }
        }
        
        private static void OnAccuracyStateBroadcast(PlayerAccuracyStateBroadcast e)
        {
            PlayerGeneralManager.GetPlayer(e.PlayerId).SetAccuracyState(e.AccuracyState);
        }

        private static void OnBalanceUpdate(PlayerBalanceUpdateBroadcast e)
        {
            var updated = PlayerGeneralManager.GetPlayer(e.PlayerId);
            updated.SetBalance(e.Balance);
            if (updated == PlayerGeneralManager.LocalPlayer)
            {
                LocalEventChannel.InvokeOnLocalPlayerBalanceUpdate(updated.Balance);
            }
        }
    }
}