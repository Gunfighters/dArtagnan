using System.Numerics;
using dArtagnan.Server.Game;
using dArtagnan.Server.Network;
using dArtagnan.Shared;

namespace dArtagnan.Server.Handlers
{
    /// <summary>
    /// 플레이어 이동 처리를 담당하는 핸들러
    /// </summary>
    public class MovementHandler
    {
        private readonly GameSession gameSession;

        public MovementHandler(GameSession gameSession)
        {
            this.gameSession = gameSession;
        }

        /// <summary>
        /// 방향에 따른 벡터를 반환합니다
        /// </summary>
        private static Vector3 GetDirectionVector(int direction)
        {
            return DirectionHelper.IntToDirection(direction);
        }

        /// <summary>
        /// 플레이어의 새로운 위치를 계산합니다
        /// </summary>
        private static (float newX, float newY) CalculateNewPosition(
            float currentX, float currentY, int direction, float speed, float deltaTime)
        {
            var vector = GetDirectionVector(direction);
            
            // 정지 상태가 아닐 때만 이동
            if (vector == Vector3.Zero)
            {
                return (currentX, currentY);
            }

            float moveX = vector.X * speed * deltaTime;
            float moveY = vector.Y * speed * deltaTime;

            return (currentX + moveX, currentY + moveY);
        }

        /// <summary>
        /// 플레이어 방향 변경을 처리합니다
        /// </summary>
        public async Task HandlePlayerDirection(PlayerDirectionFromClient directionData, ClientConnection client, Func<IPacket, Task> broadcastToAll)
        {
            var player = gameSession.GetPlayerByClientId(client.Id);
            if (player == null || !player.IsInGame) return;

            // 플레이어 상태 업데이트
            player.Direction = directionData.direction;
            player.UpdatePosition(directionData.currentX, directionData.currentY);

            Console.WriteLine($"[이동] 플레이어 {player.PlayerId} 방향: {player.Direction}, 위치: ({player.X:F2}, {player.Y:F2})");

            // 방향 변경을 모든 플레이어에게 브로드캐스트
            await broadcastToAll(new PlayerDirectionBroadcast
            {
                direction = player.Direction,
                playerId = player.PlayerId,
                currentX = player.X,
                currentY = player.Y
            });
        }

        /// <summary>
        /// 플레이어 달리기 상태 변경을 처리합니다
        /// </summary>
        public async Task HandlePlayerRunning(PlayerRunningFromClient runningData, ClientConnection client, Func<IPacket, Task> broadcastToAll)
        {
            var player = gameSession.GetPlayerByClientId(client.Id);
            if (player == null || !player.IsInGame) return;

            // 달리기 상태에 따라 속도 설정
            float newSpeed = Player.GetSpeedByRunning(runningData.isRunning);
            player.UpdateSpeed(newSpeed);

            Console.WriteLine($"[이동] 플레이어 {player.PlayerId} 달리기: {runningData.isRunning}, 속도: {player.Speed}");

            // 속도 업데이트를 모든 플레이어에게 브로드캐스트
            await broadcastToAll(new UpdatePlayerSpeedBroadcast
            {
                playerId = player.PlayerId,
                speed = player.Speed
            });
        }

        /// <summary>
        /// 게임 루프에서 호출되는 위치 업데이트 처리
        /// </summary>
        public void UpdatePlayerPositions(float deltaTime)
        {
            foreach (var player in gameSession.Players)
            {
                if (!player.Alive) continue;
                
                // 새로운 위치 계산
                var (newX, newY) = CalculateNewPosition(
                    player.X, player.Y, player.Direction, player.Speed, deltaTime);
                
                // 위치가 변경된 경우에만 업데이트
                if (Math.Abs(newX - player.X) > 0.001f || Math.Abs(newY - player.Y) > 0.001f)
                {
                    player.UpdatePosition(newX, newY);
                }
            }
        }

        /// <summary>
        /// 플레이어들의 위치 정보를 브로드캐스트합니다 (비활성화)
        /// </summary>
        // public async Task BroadcastPlayerPositions(Func<IPacket, Task> broadcastToAll)
        // {
        //     var positionList = gameSession.GetPlayersPositions();

        //     if (positionList.Count == 0) return;

        //     var packet = new UpdatePlayerPosition
        //     {
        //         positionList = positionList
        //     };

        //     await broadcastToAll(packet);
        // }
    }
} 