using System.Collections.Concurrent;
using System.Diagnostics;
using dArtagnan.Shared;

namespace dArtagnan.Server.Core
{
    /// <summary>
    /// 게임 시뮬레이션을 위한 50 FPS (0.02초 간격) 업데이트 루프를 담당하는 클래스
    /// </summary>
    public class GameLoop
    {
        private readonly GameServer gameServer;
        private bool isRunning = false;
        private const int TARGET_FPS = 50; // 0.02초 간격 (50 FPS)
        private const double TARGET_FRAME_TIME = 1000.0 / TARGET_FPS; // ms per frame (20ms)
        
        // 위치 브로드캐스트 주기 제어 (1초에 한 번)
        private int positionBroadcastCounter = 0;
        private const int POSITION_BROADCAST_INTERVAL = 50; // 50프레임 = 1초
        
        // Direction 벡터 정의 (0은 정지, 1~8 위쪽부터 시계방향)
        private readonly Dictionary<int, (float x, float y)> directionVectors = new()
        {
            { 0, (0, 0) },       // 정지
            { 1, (0, 1) },       // 위
            { 2, (1, 1) },       // 우상
            { 3, (1, 0) },       // 우
            { 4, (1, -1) },      // 우하
            { 5, (0, -1) },      // 하
            { 6, (-1, -1) },     // 좌하
            { 7, (-1, 0) },      // 좌
            { 8, (-1, 1) }       // 좌상
        };

        public GameLoop(GameServer server)
        {
            gameServer = server;
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
            UpdatePlayerPositions(deltaTime);
            UpdateReloadTimes(deltaTime);
            
            // 위치 브로드캐스트는 1초에 한 번만 실행
            positionBroadcastCounter++;
            if (positionBroadcastCounter >= POSITION_BROADCAST_INTERVAL)
            {
                BroadcastPlayerPositions();
                positionBroadcastCounter = 0;
            }
        }

        /// <summary>
        /// 모든 플레이어의 위치를 direction에 따라 업데이트
        /// </summary>
        private void UpdatePlayerPositions(float deltaTime)
        {
            foreach (var client in gameServer.clients.Values)
            {
                if (!client.IsConnected || !client.IsInGame || !client.Alive) continue;

                // Direction이 유효한 범위인지 확인
                if (!directionVectors.ContainsKey(client.Direction)) continue;

                var vector = directionVectors[client.Direction];
                
                // 정지 상태가 아닐 때만 이동
                if (vector.x != 0 || vector.y != 0)
                {
                    // 현재 속도 사용
                    float speed = client.Speed;
                    
                    // 대각선 이동 시 속도 정규화
                    if (vector.x != 0 && vector.y != 0)
                    {
                        speed /= 1.414f; // sqrt(2)
                    }
                    
                    // 고정된 deltaTime(0.02초)을 곱해서 프레임당 이동거리 계산
                    float moveX = vector.x * speed * deltaTime;
                    float moveY = vector.y * speed * deltaTime;
                    
                    // 위치 업데이트
                    client.UpdatePosition(
                        client.X + moveX,
                        client.Y + moveY
                    );
                }
            }
        }

        /// <summary>
        /// 모든 플레이어의 재장전 시간을 업데이트
        /// </summary>
        private void UpdateReloadTimes(float deltaTime)
        {
            foreach (var client in gameServer.clients.Values)
            {
                if (!client.IsConnected || !client.IsInGame || !client.Alive) continue;

                if (client.RemainingReloadTime > 0)
                {
                    float newReloadTime = Math.Max(0, client.RemainingReloadTime - deltaTime);
                    client.UpdateReloadTime(newReloadTime);
                }
            }
        }

        /// <summary>
        /// 플레이어들의 위치 정보를 모든 클라이언트에게 브로드캐스트
        /// </summary>
        private void BroadcastPlayerPositions()
        {
            // 게임에 참여 중인 플레이어가 없으면 브로드캐스트하지 않음
            var playersInGame = gameServer.clients.Values
                .Where(c => c.IsConnected && c.IsInGame)
                .ToList();
                
            if (playersInGame.Count == 0) return;

            // 위치 정보 브로드캐스트 (비동기로 실행하지만 대기하지 않음)
            _ = Task.Run(async () =>
            {
                try
                {
                    await gameServer.BroadcastPlayerPositions();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"플레이어 위치 브로드캐스트 오류: {ex.Message}");
                }
            });
        }
    }
} 