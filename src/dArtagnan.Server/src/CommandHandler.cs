namespace dArtagnan.Server;

/// <summary>
/// 서버 관리자 명령어를 처리하는 클래스
/// </summary>
public class CommandHandler(TcpServer tcpServer)
{
    private bool isRunning = true;

    /// <summary>
    /// 관리자 명령어 처리 루프를 시작합니다
    /// </summary>
    public async Task StartHandlingAsync()
    {
        Console.WriteLine("관리자 명령어:");
        Console.WriteLine("  status     - 서버 상태 출력");
        Console.WriteLine("  players    - 현재 플레이어 목록 출력 (모든 정보)");
        Console.WriteLine("  player [ID] - 특정 플레이어 정보 출력 (예: player 1)");
        Console.WriteLine("  kill [ID]  - 특정 플레이어를 죽입니다 (예: kill 1)");
        Console.WriteLine("  quit       - 서버 종료");
        Console.WriteLine();

        while (isRunning)
        {
            try
            {
                var input = Console.ReadLine();
                if (string.IsNullOrEmpty(input)) continue;

                await HandleCommandAsync(input.ToLower().Trim());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"명령어 처리 오류: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 개별 명령어를 처리합니다
    /// </summary>
    private async Task HandleCommandAsync(string command)
    {
        // 명령어를 공백으로 분리
        var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var mainCommand = parts.Length > 0 ? parts[0] : "";
        var parameters = parts.Skip(1).ToArray();

        switch (mainCommand)
        {
            case "status":
                PrintServerStatus();
                break;

            case "players":
                PrintPlayerList();
                break;

            case "player":
                if (parameters.Length > 0 && int.TryParse(parameters[0], out int playerId))
                {
                    PrintPlayer(playerId);
                }
                else
                {
                    Console.WriteLine("사용법: player [플레이어ID] (예: player 1)");
                }
                break;

            case "kill":
                if (parameters.Length > 0 && int.TryParse(parameters[0], out int targetId))
                {
                    await KillPlayer(targetId);
                }
                else
                {
                    Console.WriteLine("사용법: kill [플레이어ID] (예: kill 1)");
                }
                break;

            case "quit":
            case "exit":
                isRunning = false;
                await tcpServer.StopAsync();
                Environment.Exit(0);
                break;

            default:
                Console.WriteLine("알 수 없는 명령어입니다.");
                Console.WriteLine("사용 가능한 명령어: status, players, player [ID], kill [ID], quit");
                break;
        }
    }

    /// <summary>
    /// 특정 플레이어를 죽입니다
    /// </summary>
    private async Task KillPlayer(int playerId)
    {
        var gameManager = tcpServer.GetGameManager();
        var player = gameManager.GetPlayerById(playerId);

        if (player == null)
        {
            Console.WriteLine($"플레이어 ID {playerId}를 찾을 수 없습니다.");
            Console.WriteLine("현재 접속 중인 플레이어 ID 목록:");
            foreach (var p in gameManager.players.Values)
            {
                Console.WriteLine($"  - {p.Id}");
            }
            return;
        }

        if (!player.Alive)
        {
            Console.WriteLine($"플레이어 {playerId}({player.Nickname})는 이미 사망한 상태입니다.");
            return;
        }

        Console.WriteLine($"[관리자] 플레이어 {playerId}({player.Nickname})를 죽입니다...");
        
        // PacketHandlers의 HandlePlayerHit 메서드를 직접 사용하여 일관된 로직 적용
        await PacketHandlers.HandlePlayerHit(player, gameManager);
    }

    /// <summary>
    /// 서버 상태를 출력합니다
    /// </summary>
    private void PrintServerStatus()
    {
        var gameManager = tcpServer.GetGameManager();
            
        Console.WriteLine($"=== 서버 상태 ===");
        Console.WriteLine($"게임 상태: {gameManager.CurrentGameState}");
        Console.WriteLine($"접속 중인 클라이언트: {tcpServer.GetClientCount()}명");
        Console.WriteLine($"게임 중인 플레이어: {gameManager.players.Count}명");
        Console.WriteLine($"생존자: {gameManager.GetAlivePlayerCount()}명");

        foreach (var player in gameManager.players.Values)
        {
            string status = player.Alive ? "생존" : "사망";
            Console.WriteLine($"  플레이어 {player.Id}: {player.Nickname} ({status})");
        }

        Console.WriteLine($"================");
    }

    /// <summary>
    /// 현재 플레이어 목록과 PlayerInformation을 출력합니다
    /// </summary>
    private void PrintPlayerList()
    {
        var gameManager = tcpServer.GetGameManager();
            
        Console.WriteLine($"=== 현재 플레이어 목록 ===");
            
        if (gameManager.players.Count == 0)
        {
            Console.WriteLine("접속 중인 플레이어가 없습니다.");
            Console.WriteLine("=======================");
            return;
        }

        Console.WriteLine($"총 {gameManager.players.Count}명 접속 중");
        Console.WriteLine();

        foreach (var player in gameManager.players.Values)
        {
            PrintPlayerDetails(player, true);
            Console.WriteLine();
        }

        Console.WriteLine("=======================");
    }

    /// <summary>
    /// 특정 플레이어의 정보를 출력합니다
    /// </summary>
    private void PrintPlayer(int playerId)
    {
        var gameManager = tcpServer.GetGameManager();
        var player = gameManager.GetPlayerById(playerId);
            
        if (player == null)
        {
            Console.WriteLine($"플레이어 ID {playerId}를 찾을 수 없습니다.");
            Console.WriteLine("현재 접속 중인 플레이어 ID 목록:");
            foreach (var p in gameManager.players.Values)
            {
                Console.WriteLine($"  - {p.Id}");
            }
            return;
        }

        Console.WriteLine($"=== 플레이어 {playerId} 상세 정보 ===");
        PrintPlayerDetails(player, false);
        Console.WriteLine("================================");
    }

    /// <summary>
    /// 플레이어 정보를 출력합니다 (모든 PlayerInformation 필드 포함)
    /// </summary>
    private void PrintPlayerDetails(Player player, bool showClientHeader)
    {
        if (showClientHeader)
        {
            Console.WriteLine($"[클라이언트 ID: {player.Id}]");
        }

        if (!string.IsNullOrEmpty(player.Nickname))
        {
            Console.WriteLine($"  === PlayerInformation ===");
            Console.WriteLine($"  플레이어 ID: {player.Id}");
            Console.WriteLine($"  닉네임: {player.Nickname}");
            Console.WriteLine($"  방향: {player.MovementData.Direction}");
            Console.WriteLine($"  위치: ({player.MovementData.Position.X:F2}, {player.MovementData.Position.Y:F2})");
            Console.WriteLine($"  속도: {player.MovementData.Speed:F2}");
            Console.WriteLine($"  명중률: {player.Accuracy}%");
            Console.WriteLine($"  총 재장전 시간: {player.TotalReloadTime:F2}초");
            Console.WriteLine($"  남은 재장전 시간: {player.RemainingReloadTime:F2}초");
            Console.WriteLine($"  생존 상태: {(player.Alive ? "생존" : "사망")}");
        }
        else
        {
            Console.WriteLine($"  플레이어 정보: 미설정");
        }
    }

    /// <summary>
    /// 명령어 처리를 중지합니다
    /// </summary>
    public void Stop()
    {
        isRunning = false;
    }
}