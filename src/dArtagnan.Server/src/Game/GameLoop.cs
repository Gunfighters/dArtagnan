using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using dArtagnan.Shared;

namespace dArtagnan.Server
{
    /// <summary>
    /// 게임 시뮬레이션을 위한 50 FPS (0.02초 간격) 업데이트 루프를 담당하는 클래스
    /// </summary>
    public class GameLoop
    {
        private readonly GameServer gameServer;
        private readonly GameSession gameSession;
        private bool isRunning = false;
        private const int TARGET_FPS = 50; // 0.02초 간격 (50 FPS)
        private const double TARGET_FRAME_TIME = 1000.0 / TARGET_FPS; // ms per frame (20ms)
        
        // 위치 브로드캐스트 주기 제어 (1초에 한 번) (비활성화)
        // private int positionBroadcastCounter = 0;
        // private const int POSITION_BROADCAST_INTERVAL = 50; // 50프레임 = 1초

        public GameLoop(GameServer server, GameSession gameSession)
        {
            gameServer = server;
            this.gameSession = gameSession;
        }

        /// <summary>
        /// 게임 루프를 시작합니다
        /// </summary>
        public async Task StartAsync()
        {
            if (isRunning) return;
            
            isRunning = true;
            Console.WriteLine("게임 루프 시작 (50 FPS - 0.02초 간격)");
            await Task.Run(UpdateLoop);
        }

        /// <summary>
        /// 게임 루프를 중지합니다
        /// </summary>
        public void Stop()
        {
            isRunning = false;
            Console.WriteLine("게임 루프 중지");
        }

        /// <summary>
        /// 50 FPS (0.02초 간격)로 실행되는 메인 업데이트 루프
        /// </summary>
        private async Task UpdateLoop()
        {
            var stopwatch = Stopwatch.StartNew();
            double accumulator = 0;
            const float FIXED_DELTA_TIME = 1.0f / TARGET_FPS; // 1/50 = 0.02초
            
            while (isRunning)
            {
                var deltaTime = stopwatch.Elapsed.TotalMilliseconds;
                stopwatch.Restart();
                
                accumulator += deltaTime;
                
                // 고정 시간 간격으로 업데이트 실행 (0.02초마다)
                while (accumulator >= TARGET_FRAME_TIME)
                {
                    FixedUpdate(FIXED_DELTA_TIME); // 고정된 deltaTime 사용
                    accumulator -= TARGET_FRAME_TIME;
                }
                
                // CPU 사용률 조절을 위한 짧은 대기
                await Task.Delay(1);
            }
        }

        /// <summary>
        /// 0.02초마다 실행되는 고정 업데이트 함수
        /// </summary>
        private void FixedUpdate(float deltaTime)
        {
            // 게임 로직 업데이트 (직접 호출)
            UpdatePlayerPositions(deltaTime);
            UpdateReloadTimes(deltaTime);
            
            // 위치 브로드캐스트는 1초에 한 번만 실행 (비활성화)
            // positionBroadcastCounter++;
            // if (positionBroadcastCounter >= POSITION_BROADCAST_INTERVAL)
            // {
            //     BroadcastPlayerPositions();
            //     positionBroadcastCounter = 0;
            // }
        }

        /// <summary>
        /// 플레이어들의 위치 정보를 모든 클라이언트에게 브로드캐스트 (비활성화)
        /// </summary>
        // private void BroadcastPlayerPositions()
        // {
        //     // 비동기로 실행하지만 대기하지 않음
        //     _ = Task.Run(async () =>
        //     {
        //         try
        //         {
        //             await BroadcastPlayerPositionsAsync(gameServer.BroadcastToAll);
        //         }
        //         catch (Exception ex)
        //         {
        //             Console.WriteLine($"플레이어 위치 브로드캐스트 오류: {ex.Message}");
        //         }
        //     });
        // }

        /// <summary>
        /// 플레이어들의 위치 정보를 브로드캐스트합니다 (비활성화)
        /// </summary>
        // private async Task BroadcastPlayerPositionsAsync(Func<IPacket, Task> broadcastToAll)
        // {
        //     var positionList = gameSession.GetPlayersPositions();

        //     if (positionList.Count == 0) return;

        //     var packet = new UpdatePlayerPosition
        //     {
        //         positionList = positionList
        //     };

        //     await broadcastToAll(packet);
        // }

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
        /// 게임 루프에서 호출되는 위치 업데이트 처리
        /// </summary>
        private void UpdatePlayerPositions(float deltaTime)
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
        /// 재장전 시간을 업데이트합니다
        /// </summary>
        private static float UpdateReloadTime(float currentReloadTime, float deltaTime)
        {
            return Math.Max(0, currentReloadTime - deltaTime);
        }

        /// <summary>
        /// 게임 루프에서 호출되는 재장전 시간 업데이트
        /// </summary>
        private void UpdateReloadTimes(float deltaTime)
        {
            foreach (var player in gameSession.Players)
            {
                if (!player.Alive) continue;

                if (player.RemainingReloadTime > 0)
                {
                    float newReloadTime = UpdateReloadTime(player.RemainingReloadTime, deltaTime);
                    player.UpdateReloadTime(newReloadTime);
                }
            }
        }
    }
} 