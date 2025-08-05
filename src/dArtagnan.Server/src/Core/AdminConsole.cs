using dArtagnan.Shared;
namespace dArtagnan.Server;

/// <summary>
/// 서버 관리자 콘솔 명령어를 처리하는 클래스
/// </summary>
public class AdminConsole
{
    private GameManager gameManager;

    public AdminConsole(GameManager gameManager)
    {
        this.gameManager = gameManager;
        
        Console.WriteLine("=== 관리자 명령어 ===");
        Console.WriteLine("■ 기본 상태 조회:");
        Console.WriteLine("  status/s        - 서버 전체 상태 요약");
        Console.WriteLine("  game/g          - 게임 상세 정보 (라운드, 베팅, 판돈)");
        Console.WriteLine("  players/ps      - 현재 플레이어 목록 (모든 정보)");
        Console.WriteLine("  player/p [ID]   - 특정 플레이어 상세 정보");
        Console.WriteLine();
        Console.WriteLine("■ 전문 조회:");
        Console.WriteLine("  bots/b          - 봇 전용 정보");
        Console.WriteLine("  money/m         - 경제 시스템 상태 (잔액, 판돈)");
        Console.WriteLine("  augments/a      - 증강 시스템 상태");
        Console.WriteLine("  alive/al        - 생존자 정보");
        Console.WriteLine();
        Console.WriteLine("■ 관리:");
        Console.WriteLine("  kill/k [ID]     - 특정 플레이어를 죽입니다 (예: kill 1)");
        Console.WriteLine("  quit/q/exit     - 서버 종료");
        Console.WriteLine("  help/h/?        - 이 도움말 표시");
        Console.WriteLine();
        
        _ = Task.Run(HandleCommandsAsync);
        //(개발용) 타이머 출력 루프
        //_ = Task.Run(PrintTimerAsync);
    }

    private async Task PrintTimerAsync()
    {
        while (true)
        {
            var time = DateTime.Now.ToString("HH:mm:ss.fff");
            Console.WriteLine($"[{time}]");
            await Task.Delay(50); // 0.05초 대기
        }
    }

    /// <summary>
    /// 관리자 명령어 처리 루프
    /// </summary>
    private async Task HandleCommandsAsync()
    {
        while (true)
        {
            try
            {
                var input = Console.ReadLine();
                if (string.IsNullOrEmpty(input)) continue;

                await HandleCommandAsync(input.ToLower().Trim());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"관리자 명령어 처리 중 오류: {ex.Message}");
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
            case "s":
                PrintServerStatus();
                break;

            case "game":
            case "g":
                PrintGameDetails();
                break;

            case "players":
            case "ps":
                PrintPlayerList();
                break;

            case "player":
            case "p":
                if (parameters.Length > 0 && int.TryParse(parameters[0], out int playerId))
                {
                    PrintPlayer(playerId);
                }
                else
                {
                    Console.WriteLine("사용법: player [플레이어ID] (예: player 1)");
                }
                break;

            case "bots":
            case "b":
                PrintBotInfo();
                break;

            case "money":
            case "m":
                PrintMoneyStatus();
                break;

            case "augments":
            case "a":
                PrintAugmentStatus();
                break;

            case "alive":
            case "al":
                PrintAlivePlayersStatus();
                break;

            case "kill":
            case "k":
                if (parameters.Length > 0 && int.TryParse(parameters[0], out int targetId))
                {
                    await KillPlayer(targetId);
                }
                else
                {
                    Console.WriteLine("사용법: kill [플레이어ID] (예: kill 1)");
                }
                break;

            case "help":
            case "h":
            case "?":
                PrintHelp();
                break;

            case "quit":
            case "q":
            case "exit":
                Environment.Exit(0);
                break;

            default:
                Console.WriteLine("알 수 없는 명령어입니다. 'help' 명령어로 사용 가능한 명령어를 확인하세요.");
                break;
        }
    }

    /// <summary>
    /// 특정 플레이어를 죽입니다
    /// </summary>
    private async Task KillPlayer(int playerId)
    {
        // 직접 처리 대신 Command 생성
        await gameManager.EnqueueCommandAsync(new AdminKillPlayerCommand
        {
            TargetPlayerId = playerId
        });
    }

    /// <summary>
    /// 서버 전체 상태 요약을 출력합니다
    /// </summary>
    private void PrintServerStatus()
    {
        Console.WriteLine($"=== 서버 상태 요약 ===");
        Console.WriteLine($"현재 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"게임 상태: {gameManager.CurrentGameState}");
        
        if (gameManager.CurrentGameState == GameState.Round)
        {
            Console.WriteLine($"현재 라운드: {gameManager.Round}/{Constants.MAX_ROUNDS}");
            Console.WriteLine($"베팅금: {gameManager.BettingAmount}달러");
            Console.WriteLine($"현재 판돈: {gameManager.TotalPrizeMoney}달러");
        }
        
        Console.WriteLine();
        Console.WriteLine($"■ 참가자 현황");
        Console.WriteLine($"  총 접속자: {gameManager.Clients.Count + gameManager.Players.Values.OfType<Bot>().Count()}명");
        Console.WriteLine($"  연결된 TCP 클라이언트: {gameManager.Clients.Count}명");
        Console.WriteLine($"  봇: {gameManager.Players.Values.OfType<Bot>().Count()}명");
        Console.WriteLine($"  총 플레이어: {gameManager.Players.Count}명");
        
        var aliveCount = gameManager.Players.Values.Count(p => p.Alive);
        var bankruptCount = gameManager.Players.Values.Count(p => p.Bankrupt);
        Console.WriteLine($"  생존자: {aliveCount}명");
        Console.WriteLine($"  파산자: {bankruptCount}명");
        
        if (gameManager.Host != null)
        {
            Console.WriteLine($"  방장: {gameManager.Host.Id}번 ({gameManager.Host.Nickname})");
        }
        
        Console.WriteLine();
        Console.WriteLine($"■ 플레이어 목록");
        foreach (var player in gameManager.Players.Values.OrderBy(p => p.Id))
        {
            string type = player is Bot ? "[봇]" : "[유저]";
            string status = player.Alive ? "생존" : "사망";
            string bankrupt = player.Bankrupt ? " (파산)" : "";
            Console.WriteLine($"  {player.Id}: {type} {player.Nickname} ({status}){bankrupt} - {player.Balance}달러");
        }

        Console.WriteLine($"==================");
    }

    /// <summary>
    /// 현재 플레이어 목록과 PlayerInformation을 출력합니다
    /// </summary>
    private void PrintPlayerList()
    {
        Console.WriteLine($"=== 현재 플레이어 목록 ===");
            
        if (gameManager.Players.Count == 0)
        {
            Console.WriteLine("접속 중인 플레이어가 없습니다.");
            Console.WriteLine("=======================");
            return;
        }

        Console.WriteLine($"총 {gameManager.Players.Count}명 접속 중");
        Console.WriteLine();

        foreach (var player in gameManager.Players.Values)
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
        var player = gameManager.GetPlayerById(playerId);
            
        if (player == null)
        {
            Console.WriteLine($"플레이어 ID {playerId}를 찾을 수 없습니다.");
            Console.WriteLine("현재 접속 중인 플레이어 ID 목록:");
            foreach (var p in gameManager.Players.Values)
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
            Console.WriteLine($"  타입: {(player is Bot ? "봇" : "유저")}");
            Console.WriteLine($"  닉네임: {player.Nickname}");
            Console.WriteLine($"  방향: {player.MovementData.Direction}");
            Console.WriteLine($"  위치: ({player.MovementData.Position.X:F2}, {player.MovementData.Position.Y:F2})");
            Console.WriteLine($"  속도: {player.MovementData.Speed:F2}");
            Console.WriteLine($"  명중률: {player.Accuracy}% (상태: {GetAccuracyStateText(player.AccuracyState)})");
            Console.WriteLine($"  사거리: {player.Range:F2}");
            Console.WriteLine($"  최대 에너지: {player.EnergyData.MaxEnergy}");
            Console.WriteLine($"  현재 에너지: {player.EnergyData.CurrentEnergy:F1}");
            Console.WriteLine($"  사격 최소 필요 에너지: {player.MinEnergyToShoot}");
            Console.WriteLine($"  생존 상태: {(player.Alive ? "생존" : "사망")}");
            Console.WriteLine($"  잔액: {player.Balance}달러 {(player.Bankrupt ? "(파산)" : "")}");
            Console.WriteLine($"  타겟: {(player.Target?.Id.ToString() ?? "없음")}");
            Console.WriteLine($"  보유 증강: [{string.Join(", ", player.Augments)}]");
            Console.WriteLine($"  현재 아이템: {(player.CurrentItem == -1 ? "없음" : player.CurrentItem.ToString())}");
            if (player.IsCreatingItem)
            {
                Console.WriteLine($"  아이템 제작 중: {player.CreatingRemainingTime:F1}초 남음");
            }
        }
        else
        {
            Console.WriteLine($"  플레이어 정보: 미설정");
        }
    }

    /// <summary>
    /// 게임 상세 정보를 출력합니다
    /// </summary>
    private void PrintGameDetails()
    {
        Console.WriteLine($"=== 게임 상세 정보 ===");
        Console.WriteLine($"게임 상태: {gameManager.CurrentGameState}");
        Console.WriteLine($"현재 라운드: {gameManager.Round}/{Constants.MAX_ROUNDS}");
        
        if (gameManager.CurrentGameState == GameState.Round)
        {
            Console.WriteLine($"베팅금: {gameManager.BettingAmount}달러");
            Console.WriteLine($"베팅 타이머: {gameManager.BettingTimer:F1}초");
            Console.WriteLine($"현재 판돈: {gameManager.TotalPrizeMoney}달러");
            
            // 베팅금 배열 출력
            Console.WriteLine($"라운드별 베팅금: [{string.Join(", ", gameManager.BettingAmounts)}]달러");
        }
        
        Console.WriteLine($"룰렛 완료한 플레이어: {gameManager.rouletteDonePlayers.Count}명");
        if (gameManager.rouletteDonePlayers.Count > 0)
        {
            Console.WriteLine($"  완료자 ID: [{string.Join(", ", gameManager.rouletteDonePlayers.Select(p => p.Id))}]");
        }
        
        Console.WriteLine($"증강 선택 완료한 플레이어: {gameManager.augmentSelectionDonePlayers.Count}명");
        if (gameManager.augmentSelectionDonePlayers.Count > 0)
        {
            Console.WriteLine($"  완료자 ID: [{string.Join(", ", gameManager.augmentSelectionDonePlayers)}]");
        }
        
        Console.WriteLine($"=====================");
    }

    /// <summary>
    /// 봇 전용 정보를 출력합니다
    /// </summary>
    private void PrintBotInfo()
    {
        var bots = gameManager.Players.Values.OfType<Bot>().ToList();
        
        Console.WriteLine($"=== 봇 정보 ===");
        Console.WriteLine($"총 봇 수: {bots.Count}명");
        
        if (bots.Count == 0)
        {
            Console.WriteLine("현재 봇이 없습니다.");
            Console.WriteLine("===============");
            return;
        }
        
        Console.WriteLine();
        foreach (var bot in bots.OrderBy(b => b.Id))
        {
            Console.WriteLine($"[봇 ID: {bot.Id}] {bot.Nickname}");
            Console.WriteLine($"  생존: {(bot.Alive ? "생존" : "사망")}");
            Console.WriteLine($"  잔액: {bot.Balance}달러 {(bot.Bankrupt ? "(파산)" : "")}");
            Console.WriteLine($"  정확도: {bot.Accuracy}% (상태: {GetAccuracyStateText(bot.AccuracyState)})");
            Console.WriteLine($"  위치: ({bot.MovementData.Position.X:F2}, {bot.MovementData.Position.Y:F2})");
            Console.WriteLine($"  에너지: {bot.EnergyData.CurrentEnergy:F1}/{bot.EnergyData.MaxEnergy}");
            Console.WriteLine($"  타겟: {(bot.Target?.Id.ToString() ?? "없음")}");
            Console.WriteLine();
        }
        
        Console.WriteLine("===============");
    }

    /// <summary>
    /// 경제 시스템 상태를 출력합니다
    /// </summary>
    private void PrintMoneyStatus()
    {
        Console.WriteLine($"=== 경제 시스템 상태 ===");
        Console.WriteLine($"현재 판돈: {gameManager.TotalPrizeMoney}달러");
        
        if (gameManager.CurrentGameState == GameState.Round)
        {
            Console.WriteLine($"현재 베팅금: {gameManager.BettingAmount}달러");
            Console.WriteLine($"베팅 타이머: {gameManager.BettingTimer:F1}초");
        }
        
        Console.WriteLine();
        Console.WriteLine($"■ 플레이어별 잔액");
        
        var players = gameManager.Players.Values.OrderByDescending(p => p.Balance).ToList();
        if (players.Count == 0)
        {
            Console.WriteLine("플레이어가 없습니다.");
        }
        else
        {
            int rank = 1;
            foreach (var player in players)
            {
                string type = player is Bot ? "[봇]" : "[유저]";
                string status = "";
                if (player.Bankrupt) status += " (파산)";
                if (!player.Alive) status += " (사망)";
                
                Console.WriteLine($"  {rank}위: {type} {player.Nickname} - {player.Balance}달러{status}");
                rank++;
            }
            
            var totalMoney = players.Sum(p => p.Balance);
            var avgMoney = players.Count > 0 ? totalMoney / players.Count : 0;
            Console.WriteLine();
            Console.WriteLine($"총 플레이어 보유금: {totalMoney}달러");
            Console.WriteLine($"평균 보유금: {avgMoney:F1}달러");
            Console.WriteLine($"파산자: {players.Count(p => p.Bankrupt)}명");
        }
        
        Console.WriteLine($"========================");
    }

    /// <summary>
    /// 증강 시스템 상태를 출력합니다
    /// </summary>
    private void PrintAugmentStatus()
    {
        Console.WriteLine($"=== 증강 시스템 상태 ===");
        Console.WriteLine($"게임 상태: {gameManager.CurrentGameState}");
        
        Console.WriteLine();
        Console.WriteLine($"■ 룰렛 상태");
        Console.WriteLine($"룰렛 완료한 플레이어: {gameManager.rouletteDonePlayers.Count}명");
        if (gameManager.rouletteDonePlayers.Count > 0)
        {
            foreach (var player in gameManager.rouletteDonePlayers)
            {
                Console.WriteLine($"  - {player.Id}번 {player.Nickname}");
            }
        }
        
        Console.WriteLine();
        Console.WriteLine($"■ 증강 선택 상태");
        Console.WriteLine($"증강 선택 완료한 플레이어: {gameManager.augmentSelectionDonePlayers.Count}명");
        if (gameManager.augmentSelectionDonePlayers.Count > 0)
        {
            Console.WriteLine($"  완료자 ID: [{string.Join(", ", gameManager.augmentSelectionDonePlayers)}]");
        }
        
        Console.WriteLine();
        Console.WriteLine($"■ 플레이어별 증강 옵션");
        if (gameManager.playerAugmentOptions.Count == 0)
        {
            Console.WriteLine("현재 증강 선택 단계가 아닙니다.");
        }
        else
        {
            foreach (var kvp in gameManager.playerAugmentOptions)
            {
                var player = gameManager.GetPlayerById(kvp.Key);
                var playerName = player?.Nickname ?? "알 수 없음";
                Console.WriteLine($"  {kvp.Key}번 {playerName}: [{string.Join(", ", kvp.Value)}]");
            }
        }
        
        Console.WriteLine();
        Console.WriteLine($"■ 플레이어별 보유 증강");
        foreach (var player in gameManager.Players.Values.OrderBy(p => p.Id))
        {
            if (player.Augments.Count > 0)
            {
                Console.WriteLine($"  {player.Id}번 {player.Nickname}: [{string.Join(", ", player.Augments)}]");
            }
        }
        
        Console.WriteLine($"========================");
    }

    /// <summary>
    /// 생존자 정보를 출력합니다
    /// </summary>
    private void PrintAlivePlayersStatus()
    {
        var alivePlayers = gameManager.Players.Values.Where(p => p.Alive).OrderBy(p => p.Id).ToList();
        var deadPlayers = gameManager.Players.Values.Where(p => !p.Alive).OrderBy(p => p.Id).ToList();
        
        Console.WriteLine($"=== 생존자 정보 ===");
        Console.WriteLine($"생존자: {alivePlayers.Count}명, 사망자: {deadPlayers.Count}명");
        
        Console.WriteLine();
        Console.WriteLine($"■ 생존자 목록");
        if (alivePlayers.Count == 0)
        {
            Console.WriteLine("생존자가 없습니다.");
        }
        else
        {
            foreach (var player in alivePlayers)
            {
                string type = player is Bot ? "[봇]" : "[유저]";
                string bankrupt = player.Bankrupt ? " (파산)" : "";
                Console.WriteLine($"  {player.Id}번: {type} {player.Nickname} - {player.Balance}달러{bankrupt}");
                Console.WriteLine($"    정확도: {player.Accuracy}%, 에너지: {player.EnergyData.CurrentEnergy:F1}/{player.EnergyData.MaxEnergy}");
            }
        }
        
        Console.WriteLine();
        Console.WriteLine($"■ 사망자 목록");
        if (deadPlayers.Count == 0)
        {
            Console.WriteLine("사망자가 없습니다.");
        }
        else
        {
            foreach (var player in deadPlayers)
            {
                string type = player is Bot ? "[봇]" : "[유저]";
                string bankrupt = player.Bankrupt ? " (파산으로 사망)" : " (전투 중 사망)";
                Console.WriteLine($"  {player.Id}번: {type} {player.Nickname} - {player.Balance}달러{bankrupt}");
            }
        }
        
        Console.WriteLine($"==================");
    }

    /// <summary>
    /// 도움말을 출력합니다
    /// </summary>
    private void PrintHelp()
    {
        Console.WriteLine("=== 관리자 명령어 도움말 ===");
        Console.WriteLine("■ 기본 상태 조회:");
        Console.WriteLine("  status/s        - 서버 전체 상태 요약");
        Console.WriteLine("  game/g          - 게임 상세 정보 (라운드, 베팅, 판돈)");
        Console.WriteLine("  players/ps      - 현재 플레이어 목록 (모든 정보)");
        Console.WriteLine("  player/p [ID]   - 특정 플레이어 상세 정보");
        Console.WriteLine();
        Console.WriteLine("■ 전문 조회:");
        Console.WriteLine("  bots/b          - 봇 전용 정보");
        Console.WriteLine("  money/m         - 경제 시스템 상태 (잔액, 판돈)");
        Console.WriteLine("  augments/a      - 증강 시스템 상태");
        Console.WriteLine("  alive/al        - 생존자 정보");
        Console.WriteLine();
        Console.WriteLine("■ 관리:");
        Console.WriteLine("  kill/k [ID]     - 특정 플레이어를 죽입니다 (예: kill 1)");
        Console.WriteLine("  quit/q/exit     - 서버 종료");
        Console.WriteLine("  help/h/?        - 이 도움말 표시");
        Console.WriteLine("============================");
    }

    /// <summary>
    /// 정확도 상태를 텍스트로 변환합니다
    /// </summary>
    private string GetAccuracyStateText(int accuracyState)
    {
        return accuracyState switch
        {
            -1 => "감소",
            0 => "유지",
            1 => "증가",
            _ => "알 수 없음"
        };
    }
}