using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using dArtagnan.Server.Handlers;
using dArtagnan.Shared;

namespace dArtagnan.Server.Core
{
    /// <summary>
    /// 게임 시뮬레이션을 위한 50 FPS (0.02초 간격) 업데이트 루프를 담당하는 클래스
    /// </summary>
    public class GameLoop
    {
        private readonly GameServer gameServer;
        private readonly MovementHandler movementHandler;
        private readonly CombatHandler combatHandler;
        private bool isRunning = false;
        private const int TARGET_FPS = 50; // 0.02초 간격 (50 FPS)
        private const double TARGET_FRAME_TIME = 1000.0 / TARGET_FPS; // ms per frame (20ms)
        
        // 위치 브로드캐스트 주기 제어 (1초에 한 번) (비활성화)
        // private int positionBroadcastCounter = 0;
        // private const int POSITION_BROADCAST_INTERVAL = 50; // 50프레임 = 1초

        public GameLoop(GameServer server, MovementHandler movementHandler, CombatHandler combatHandler)
        {
            gameServer = server;
            this.movementHandler = movementHandler;
            this.combatHandler = combatHandler;
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
            movementHandler.UpdatePlayerPositions(deltaTime);
            combatHandler.UpdateReloadTimes(deltaTime);
            
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
        //             await movementHandler.BroadcastPlayerPositions(gameServer.BroadcastToAll);
        //         }
        //         catch (Exception ex)
        //         {
        //             Console.WriteLine($"플레이어 위치 브로드캐스트 오류: {ex.Message}");
        //         }
        //     });
        // }
    }
} 