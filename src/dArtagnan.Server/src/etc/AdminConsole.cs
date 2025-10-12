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
        
        Logger.log("=== 관리자 명령어 ===");
        Logger.log("■ 기본 상태 조회:");
        Logger.log("  status/s        - 서버 전체 상태 요약");
        Logger.log("  game/g          - 게임 상세 정보 (라운드, 베팅, 판돈)");
        Logger.log("  players/ps      - 현재 플레이어 목록 (모든 정보)");
        Logger.log("  player/p [ID]   - 특정 플레이어 상세 정보");
        Logger.log();
        Logger.log("■ 전문 조회:");
        Logger.log("  bots/b          - 봇 전용 정보");
        Logger.log("  money/m         - 경제 시스템 상태 (잔액, 판돈)");
        Logger.log("  augments/a      - 증강 시스템 상태");
        Logger.log("  alive/al        - 생존자 정보");
        Logger.log();
        Logger.log("■ 관리:");
        Logger.log("  kill/k [ID]     - 특정 플레이어를 죽입니다 (예: kill 1)");
        Logger.log("  quit/q/exit     - 서버 종료");
        Logger.log("  help/h/?        - 이 도움말 표시");
        Logger.log();
        
        _ = Task.Run(HandleCommandsAsync);
        //(개발용) 타이머 출력 루프
        //_ = Task.Run(PrintTimerAsync);
    }

    private async Task PrintTimerAsync()
    {
        while (true)
        {
            var time = DateTime.Now.ToString("HH:mm:ss.fff");
            Logger.log($"[{time}]");
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
                Logger.log($"관리자 명령어 처리 중 오류: {ex.Message}");
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
                    Logger.log("사용법: player [플레이어ID] (예: player 1)");
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
                    gameManager.EnqueueCommandAsync(new AdminKillCommand
                    {
                        PlayerId = targetId
                    });
                }
                else
                {
                    Logger.log("사용법: kill [플레이어ID] (예: kill 1)");
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
                Logger.log("알 수 없는 명령어입니다. 'help' 명령어로 사용 가능한 명령어를 확인하세요.");
                break;
        }
    }

    /// <summary>
    /// 서버 전체 상태 요약을 출력합니다
    /// </summary>
    private void PrintServerStatus()
    {
        Logger.log($"=== 서버 상태 요약 ===");
        Logger.log($"현재 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Logger.log($"게임 상태: {gameManager.CurrentGameState}");
        
        if (gameManager.CurrentGameState == GameState.Round)
        {
            Logger.log($"현재 라운드: {gameManager.Round}/{Constants.MAX_ROUNDS}");
            Logger.log($"베팅금: {gameManager.BaseBettingAmount}달러");
            Logger.log($"현재 판돈: {gameManager.TotalPrizeMoney}달러");
        }
        
        Logger.log();
        Logger.log($"■ 참가자 현황");
        Logger.log($"  총 접속자: {gameManager.Clients.Count + gameManager.Players.Values.OfType<Bot>().Count()}명");
        Logger.log($"  연결된 TCP 클라이언트: {gameManager.Clients.Count}명");
        Logger.log($"  봇: {gameManager.Players.Values.OfType<Bot>().Count()}명");
        Logger.log($"  총 플레이어: {gameManager.Players.Count}명");
        
        var aliveCount = gameManager.Players.Values.Count(p => p.Alive);
        var bankruptCount = gameManager.Players.Values.Count(p => p.Bankrupt);
        Logger.log($"  생존자: {aliveCount}명");
        Logger.log($"  파산자: {bankruptCount}명");
        
        if (gameManager.Host != null)
        {
            Logger.log($"  방장: {gameManager.Host.Id}번 ({gameManager.Host.Nickname})");
        }
        
        Logger.log();
        Logger.log($"■ 플레이어 목록");
        foreach (var player in gameManager.Players.Values.OrderBy(p => p.Id))
        {
            string type = player is Bot ? "[봇]" : "[유저]";
            string status = player.Alive ? "생존" : "사망";
            string bankrupt = player.Bankrupt ? " (파산)" : "";
            Logger.log($"  {player.Id}: {type} {player.Nickname} ({status}){bankrupt} - {player.Balance}달러");
        }

        Logger.log($"==================");
    }

    /// <summary>
    /// 현재 플레이어 목록과 PlayerInformation을 출력합니다
    /// </summary>
    private void PrintPlayerList()
    {
        Logger.log($"=== 현재 플레이어 목록 ===");
            
        if (gameManager.Players.Count == 0)
        {
            Logger.log("접속 중인 플레이어가 없습니다.");
            Logger.log("=======================");
            return;
        }

        Logger.log($"총 {gameManager.Players.Count}명 접속 중");
        Logger.log();

        foreach (var player in gameManager.Players.Values)
        {
            PrintPlayerDetails(player, true);
            Logger.log();
        }

        Logger.log("=======================");
    }

    /// <summary>
    /// 특정 플레이어의 정보를 출력합니다
    /// </summary>
    private void PrintPlayer(int playerId)
    {
        var player = gameManager.GetPlayerById(playerId);
            
        if (player == null)
        {
            Logger.log($"플레이어 ID {playerId}를 찾을 수 없습니다.");
            Logger.log("현재 접속 중인 플레이어 ID 목록:");
            foreach (var p in gameManager.Players.Values)
            {
                Logger.log($"  - {p.Id}");
            }
            return;
        }

        Logger.log($"=== 플레이어 {playerId} 상세 정보 ===");
        PrintPlayerDetails(player, false);
        Logger.log("================================");
    }

    /// <summary>
    /// 플레이어 정보를 출력합니다 (모든 PlayerInformation 필드 포함)
    /// </summary>
    private void PrintPlayerDetails(Player player, bool showClientHeader)
    {
        if (showClientHeader)
        {
            Logger.log($"[클라이언트 ID: {player.Id}]");
        }

        if (!string.IsNullOrEmpty(player.Nickname))
        {
            Logger.log($"  === PlayerInformation ===");
            Logger.log($"  플레이어 ID: {player.Id}");
            Logger.log($"  타입: {(player is Bot ? "봇" : "유저")}");
            Logger.log($"  닉네임: {player.Nickname}");
            Logger.log($"  방향: {player.Direction}");
            Logger.log($"  위치: ({player.Position.X:F2}, {player.Position.Y:F2})");
            Logger.log($"  속도: {player.Speed:F2}");
            Logger.log($"  명중률: {player.Accuracy}% (상태: {GetAccuracyStateText(player.AccuracyState)})");
            Logger.log($"  사거리: {player.Range:F2}");
            Logger.log($"  총 재장전시간: {player.TotalReloadTime:F1}초");
            Logger.log($"  현재 재장전시간: {player.RemainingReloadTime:F1}초");
            Logger.log($"  생존 상태: {(player.Alive ? "생존" : "사망")}");
            Logger.log($"  파산 상태: {(player.Bankrupt ? "O" : "X")}");
            Logger.log($"  잔액: {player.Balance}달러");
            Logger.log($"  타겟: {(player.Target?.Id.ToString() ?? "없음")}");
            Logger.log($"  보유 아이템: [{string.Join(", ", player.Items)}]");
            if (player.IsMining)
            {
                Logger.log($"  채굴 중: {player.MiningRemainingTime:F1}초 남음");
            }
        }
        else
        {
            Logger.log($"  플레이어 정보: 미설정");
        }
    }

    /// <summary>
    /// 게임 상세 정보를 출력합니다
    /// </summary>
    private void PrintGameDetails()
    {
        Logger.log($"=== 게임 상세 정보 ===");
        Logger.log($"게임 상태: {gameManager.CurrentGameState}");
        Logger.log($"현재 라운드: {gameManager.Round}/{Constants.MAX_ROUNDS}");
        
        if (gameManager.CurrentGameState == GameState.Round)
        {
            Logger.log($"베팅금: {gameManager.BaseBettingAmount}달러");
            Logger.log($"베팅 타이머: {gameManager.BettingTimer:F1}초");
            Logger.log($"현재 판돈: {gameManager.TotalPrizeMoney}달러");
            
            // 베팅금 배열 출력
            Logger.log($"라운드별 베팅금: [{string.Join(", ", Constants.BETTING_AMOUNTS)}]달러");
        }
        
        // 룰렛 관련 정보는 자동화로 인해 제거됨
        
        Logger.log($"이니셜 룰렛 완료한 플레이어: {gameManager.initialRouletteCompletedPlayers.Count}명");
        if (gameManager.initialRouletteCompletedPlayers.Count > 0)
        {
            Logger.log($"  완료자 ID: [{string.Join(", ", gameManager.initialRouletteCompletedPlayers)}]");
        }
        
        Logger.log($"=====================");
    }

    /// <summary>
    /// 봇 전용 정보를 출력합니다
    /// </summary>
    private void PrintBotInfo()
    {
        var bots = gameManager.Players.Values.OfType<Bot>().ToList();
        
        Logger.log($"=== 봇 정보 ===");
        Logger.log($"총 봇 수: {bots.Count}명");
        
        if (bots.Count == 0)
        {
            Logger.log("현재 봇이 없습니다.");
            Logger.log("===============");
            return;
        }
        
        Logger.log();
        foreach (var bot in bots.OrderBy(b => b.Id))
        {
            Logger.log($"[봇 ID: {bot.Id}] {bot.Nickname}");
            Logger.log($"  생존: {(bot.Alive ? "생존" : "사망")}");
            Logger.log($"  잔액: {bot.Balance}달러 {(bot.Bankrupt ? "(파산)" : "")}");
            Logger.log($"  정확도: {bot.Accuracy}% (상태: {GetAccuracyStateText(bot.AccuracyState)})");
            Logger.log($"  위치: ({bot.Position.X:F2}, {bot.Position.Y:F2})");
            Logger.log($"  재장전시간: {bot.RemainingReloadTime:F1}/{bot.TotalReloadTime:F1}");
            Logger.log($"  타겟: {(bot.Target?.Id.ToString() ?? "없음")}");
            Logger.log();
        }
        
        Logger.log("===============");
    }

    /// <summary>
    /// 경제 시스템 상태를 출력합니다
    /// </summary>
    private void PrintMoneyStatus()
    {
        Logger.log($"=== 경제 시스템 상태 ===");
        Logger.log($"현재 판돈: {gameManager.TotalPrizeMoney}달러");
        
        if (gameManager.CurrentGameState == GameState.Round)
        {
            Logger.log($"현재 베팅금: {gameManager.BaseBettingAmount}달러");
            Logger.log($"베팅 타이머: {gameManager.BettingTimer:F1}초");
        }
        
        Logger.log();
        Logger.log($"■ 플레이어별 잔액");
        
        var players = gameManager.Players.Values.OrderByDescending(p => p.Balance).ToList();
        if (players.Count == 0)
        {
            Logger.log("플레이어가 없습니다.");
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
                
                Logger.log($"  {rank}위: {type} {player.Nickname} - {player.Balance}달러{status}");
                rank++;
            }
            
            var totalMoney = players.Sum(p => p.Balance);
            var avgMoney = players.Count > 0 ? totalMoney / players.Count : 0;
            Logger.log();
            Logger.log($"총 플레이어 보유금: {totalMoney}달러");
            Logger.log($"평균 보유금: {avgMoney:F1}달러");
            Logger.log($"파산자: {players.Count(p => p.Bankrupt)}명");
        }
        
        Logger.log($"========================");
    }

    /// <summary>
    /// 증강 시스템 상태를 출력합니다
    /// </summary>
    private void PrintAugmentStatus()
    {
        Logger.log($"=== 증강 시스템 상태 ===");
        Logger.log($"게임 상태: {gameManager.CurrentGameState}");
        
        Logger.log();
        Logger.log($"■ 게임 쇼다운 상태");
        Logger.log("게임 준비는 자동으로 진행됩니다 (3초 후 자동 시작)");
        
        Logger.log();
        Logger.log($"■ 증강 선택 상태");
        Logger.log($"이니셜 룰렛 완료한 플레이어: {gameManager.initialRouletteCompletedPlayers.Count}명");
        if (gameManager.initialRouletteCompletedPlayers.Count > 0)
        {
            Logger.log($"  완료자 ID: [{string.Join(", ", gameManager.initialRouletteCompletedPlayers)}]");
        }
        
        Logger.log();
        Logger.log($"■ 플레이어별 상점 데이터");
        if (gameManager.CurrentGameState != GameState.Shop)
        {
            Logger.log("현재 상점 단계가 아닙니다.");
        }
        else
        {
            foreach (var kvp in gameManager.Players)
            {
                var player = kvp.Value;
                var shopData = player.ShopData;
                Logger.log($"  {kvp.Key}번 {player.Nickname}: 아이템 {shopData.YourItems.Count}개, 룰렛 {shopData.ShopRoulettePrice}달러");
            }
        }
        
        Logger.log();
        Logger.log($"■ 플레이어별 보유 아이템");
        foreach (var player in gameManager.Players.Values.OrderBy(p => p.Id))
        {
            if (player.Items.Count > 0)
            {
                Logger.log($"  {player.Id}번 {player.Nickname}: [{string.Join(", ", player.Items)}]");
            }
        }
        
        Logger.log($"========================");
    }

    /// <summary>
    /// 생존자 정보를 출력합니다
    /// </summary>
    private void PrintAlivePlayersStatus()
    {
        var alivePlayers = gameManager.Players.Values.Where(p => p.Alive).OrderBy(p => p.Id).ToList();
        var deadPlayers = gameManager.Players.Values.Where(p => !p.Alive).OrderBy(p => p.Id).ToList();
        
        Logger.log($"=== 생존자 정보 ===");
        Logger.log($"생존자: {alivePlayers.Count}명, 사망자: {deadPlayers.Count}명");
        
        Logger.log();
        Logger.log($"■ 생존자 목록");
        if (alivePlayers.Count == 0)
        {
            Logger.log("생존자가 없습니다.");
        }
        else
        {
            foreach (var player in alivePlayers)
            {
                string type = player is Bot ? "[봇]" : "[유저]";
                string bankrupt = player.Bankrupt ? " (파산)" : "";
                Logger.log($"  {player.Id}번: {type} {player.Nickname} - {player.Balance}달러{bankrupt}");
                Logger.log($"    정확도: {player.Accuracy}%, 재장전시간: {player.RemainingReloadTime:F1}/{player.TotalReloadTime:F1}");
            }
        }
        
        Logger.log();
        Logger.log($"■ 사망자 목록");
        if (deadPlayers.Count == 0)
        {
            Logger.log("사망자가 없습니다.");
        }
        else
        {
            foreach (var player in deadPlayers)
            {
                string type = player is Bot ? "[봇]" : "[유저]";
                string bankrupt = player.Bankrupt ? " (파산으로 사망)" : " (전투 중 사망)";
                Logger.log($"  {player.Id}번: {type} {player.Nickname} - {player.Balance}달러{bankrupt}");
            }
        }
        
        Logger.log($"==================");
    }

    /// <summary>
    /// 도움말을 출력합니다
    /// </summary>
    private void PrintHelp()
    {
        Logger.log("=== 관리자 명령어 도움말 ===");
        Logger.log("■ 기본 상태 조회:");
        Logger.log("  status/s        - 서버 전체 상태 요약");
        Logger.log("  game/g          - 게임 상세 정보 (라운드, 베팅, 판돈)");
        Logger.log("  players/ps      - 현재 플레이어 목록 (모든 정보)");
        Logger.log("  player/p [ID]   - 특정 플레이어 상세 정보");
        Logger.log();
        Logger.log("■ 전문 조회:");
        Logger.log("  bots/b          - 봇 전용 정보");
        Logger.log("  money/m         - 경제 시스템 상태 (잔액, 판돈)");
        Logger.log("  augments/a      - 증강 시스템 상태");
        Logger.log("  alive/al        - 생존자 정보");
        Logger.log();
        Logger.log("■ 관리:");
        Logger.log("  kill/k [ID]     - 특정 플레이어를 죽입니다 (예: kill 1)");
        Logger.log("  quit/q/exit     - 서버 종료");
        Logger.log("  help/h/?        - 이 도움말 표시");
        Logger.log("============================");
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