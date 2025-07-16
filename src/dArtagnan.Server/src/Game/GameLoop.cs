using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 게임 시뮬레이션을 위한 50 FPS (0.02초 간격) 업데이트 루프를 담당하는 클래스
/// </summary>
public class GameLoop(GameManager gameManager)
{
    private const int TARGET_FPS = 50; // 0.02초 간격 (50 FPS)
    private const double TARGET_FRAME_TIME = 1000.0 / TARGET_FPS; // ms per frame (20ms)
    
    /// <summary>
    /// 게임 루프를 시작합니다
    /// </summary>
    public async Task StartAsync()
    {
        Console.WriteLine("게임 루프 시작 (50 FPS - 0.02초 간격)");
        
        // UpdateLoop 시작
        await Task.Run(UpdateLoop);
    }

    /// <summary>
    /// 50 FPS (0.02초 간격)로 실행되는 메인 업데이트 루프
    /// </summary>
    private async Task UpdateLoop()
    {
        var stopwatch = Stopwatch.StartNew();
        double accumulator = 0;
        const float FIXED_DELTA_TIME = 1.0f / TARGET_FPS; // 1/50 = 0.02초
            
        while (true)
        {
            var deltaTime = stopwatch.Elapsed.TotalMilliseconds;
            stopwatch.Restart();
                
            accumulator += deltaTime;
                
            // 고정 시간 간격으로 업데이트 실행 (0.02초마다)
            while (accumulator >= TARGET_FRAME_TIME)
            {
                // 게임 상태 업데이트 명령을 큐에 추가
                await gameManager.EnqueueCommandAsync(new UpdateGameStateCommand 
                { 
                    DeltaTime = FIXED_DELTA_TIME 
                });
                
                accumulator -= TARGET_FRAME_TIME;
            }
                
            // CPU 사용률 조절을 위한 짧은 대기
            await Task.Delay(1);
        }
    }


}