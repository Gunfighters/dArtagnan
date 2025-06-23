using System;
using System.Linq;
using System.Threading.Tasks;

namespace dArtagnan.Server
{
    /// <summary>
    /// 서버 관리자 명령어를 처리하는 클래스
    /// </summary>
    public class CommandHandler
    {
        private readonly TcpServer tcpServer;
        private bool isRunning = true;

        public CommandHandler(TcpServer tcpServer)
        {
            this.tcpServer = tcpServer;
        }

        /// <summary>
        /// 관리자 명령어 처리 루프를 시작합니다
        /// </summary>
        public async Task StartHandlingAsync()
        {
            Console.WriteLine("관리자 명령어:");
            Console.WriteLine("  status     - 서버 상태 출력");
            Console.WriteLine("  players    - 현재 플레이어 목록 출력 (모든 정보)");
            Console.WriteLine("  player [ID] - 특정 플레이어 정보 출력 (예: player 1)");
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

                case "quit":
                case "exit":
                    isRunning = false;
                    await tcpServer.StopAsync();
                    Environment.Exit(0);
                    break;

                default:
                    Console.WriteLine("알 수 없는 명령어입니다.");
                    Console.WriteLine("사용 가능한 명령어: status, players, player [ID], quit");
                    break;
            }
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
            Console.WriteLine($"게임 중인 플레이어: {gameManager.Players.Count()}명");
            Console.WriteLine($"Ready 플레이어: {gameManager.GetReadyPlayerCount()}명");
            Console.WriteLine($"생존자: {gameManager.GetAlivePlayerCount()}명");

            foreach (var player in gameManager.Players)
            {
                string status = player.Alive ? "생존" : "사망";
                string readyStatus = player.IsReady ? "Ready" : "Not Ready";
                Console.WriteLine($"  플레이어 {player.PlayerId}: {player.Nickname} ({status}, {readyStatus})");
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
            
            if (gameManager.PlayerCount == 0)
            {
                Console.WriteLine("접속 중인 플레이어가 없습니다.");
                Console.WriteLine("=======================");
                return;
            }

            Console.WriteLine($"총 {gameManager.PlayerCount}명 접속 중");
            Console.WriteLine();

            foreach (var player in gameManager.AllPlayers)
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
            var player = gameManager.GetPlayerByPlayerId(playerId);
            
            if (player == null)
            {
                Console.WriteLine($"플레이어 ID {playerId}를 찾을 수 없습니다.");
                Console.WriteLine("현재 접속 중인 플레이어 ID 목록:");
                foreach (var p in gameManager.AllPlayers)
                {
                    Console.WriteLine($"  - {p.PlayerId}");
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
            
            Console.WriteLine($"  게임 상태: {(player.IsInGame ? "게임 중" : "대기 중")}");
            
            if (!string.IsNullOrEmpty(player.Nickname))
            {
                Console.WriteLine($"  === PlayerInformation ===");
                Console.WriteLine($"  플레이어 ID: {player.PlayerId}");
                Console.WriteLine($"  닉네임: {player.Nickname}");
                Console.WriteLine($"  Ready 상태: {(player.IsReady ? "준비 완료" : "준비 중")}");
                Console.WriteLine($"  방향: {player.Direction}");
                Console.WriteLine($"  위치 X: {player.X:F2}");
                Console.WriteLine($"  위치 Y: {player.Y:F2}");
                Console.WriteLine($"  명중률: {player.Accuracy}%");
                Console.WriteLine($"  속도: {player.Speed:F2}");
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
} 