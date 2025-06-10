using System;
using System.Linq;
using System.Threading.Tasks;

namespace dArtagnan.Server.Core
{
    /// <summary>
    /// 서버 관리자 명령어를 처리하는 클래스
    /// </summary>
    public class CommandHandler
    {
        private readonly GameServer gameServer;
        private bool isRunning = true;

        public CommandHandler(GameServer gameServer)
        {
            this.gameServer = gameServer;
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

            switch (mainCommand)
            {
                case "status":
                    PrintServerStatus();
                    break;

                case "players":
                    PrintPlayerList();
                    break;

                case "player":
                    if (parts.Length > 1 && int.TryParse(parts[1], out int playerId))
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
                    await gameServer.StopAsync();
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
            Console.WriteLine($"=== 서버 상태 ===");
            Console.WriteLine($"접속 중인 클라이언트: {gameServer.clients.Count}명");

            var gameClients = gameServer.clients.Values.Where(c => c.IsInGame).ToList();
            Console.WriteLine($"게임 중인 플레이어: {gameClients.Count}명");

            foreach (var client in gameClients)
            {
                Console.WriteLine($"  플레이어 {client.Id}: {client.playerinfo.nickname}");
            }

            Console.WriteLine($"================");
        }

        /// <summary>
        /// 현재 플레이어 목록과 PlayerInformation을 출력합니다
        /// </summary>
        private void PrintPlayerList()
        {
            Console.WriteLine($"=== 현재 플레이어 목록 ===");
            
            if (gameServer.clients.Count == 0)
            {
                Console.WriteLine("접속 중인 플레이어가 없습니다.");
                Console.WriteLine("=======================");
                return;
            }

            Console.WriteLine($"총 {gameServer.clients.Count}명 접속 중");
            Console.WriteLine();

            foreach (var client in gameServer.clients.Values)
            {
                PrintPlayerInfo(client, true);
                Console.WriteLine();
            }

            Console.WriteLine("=======================");
        }

        /// <summary>
        /// 특정 플레이어의 정보를 출력합니다
        /// </summary>
        private void PrintPlayer(int playerId)
        {
            var client = gameServer.clients.Values.FirstOrDefault(c => c.Id == playerId);
            
            if (client == null)
            {
                Console.WriteLine($"플레이어 ID {playerId}를 찾을 수 없습니다.");
                Console.WriteLine("현재 접속 중인 플레이어 ID 목록:");
                foreach (var c in gameServer.clients.Values)
                {
                    Console.WriteLine($"  - {c.Id}");
                }
                return;
            }

            Console.WriteLine($"=== 플레이어 {playerId} 상세 정보 ===");
            PrintPlayerInfo(client, false);
            Console.WriteLine("================================");
        }

        /// <summary>
        /// 플레이어 정보를 출력합니다 (모든 PlayerInformation 필드 포함)
        /// </summary>
        private void PrintPlayers(ClientConnection client, bool showClientHeader)
        {
            if (showClientHeader)
            {
                Console.WriteLine($"[클라이언트 ID: {client.Id}]");
            }
            
            Console.WriteLine($"  연결 상태: {(client.IsConnected ? "연결됨" : "끊김")}");
            Console.WriteLine($"  게임 상태: {(client.IsInGame ? "게임 중" : "대기 중")}");
            
            if (!string.IsNullOrEmpty(client.playerinfo.nickname))
            {
                Console.WriteLine($"  === PlayerInformation ===");
                Console.WriteLine($"  플레이어 ID: {client.playerinfo.playerId}");
                Console.WriteLine($"  닉네임: {client.playerinfo.nickname}");
                Console.WriteLine($"  방향: {client.playerinfo.direction}");
                Console.WriteLine($"  위치 X: {client.playerinfo.x}");
                Console.WriteLine($"  위치 Y: {client.playerinfo.y}");
                Console.WriteLine($"  명중률: {client.playerinfo.accuracy}%");
                Console.WriteLine($"  달리기 상태: {(client.playerinfo.isRunning ? "달리는 중" : "정지")}");
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