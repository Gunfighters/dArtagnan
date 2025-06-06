using dArtagnan.Server.Core;

namespace dArtagnan.Server
{
    class Program
    {
        private static GameServer gameServer = new GameServer();
        private static bool isRunning = true;

        static async Task Main(string[] args)
        {
            Console.WriteLine("D'Artagnan 게임 서버");
            Console.WriteLine("==================");

            // 종료 시그널 처리
            Console.CancelKeyPress += async (sender, e) =>
            {
                e.Cancel = true;
                isRunning = false;
                await gameServer.StopAsync();
                Environment.Exit(0);
            };

            // 서버 시작
            int port = 7777;
            var serverTask = gameServer.StartAsync(port);

            // 관리자 명령어 처리
            _ = Task.Run(HandleAdminCommands);

            await serverTask;
        }

        private static async Task HandleAdminCommands()
        {
            Console.WriteLine("관리자 명령어:");
            Console.WriteLine("  status - 서버 상태 출력");
            Console.WriteLine("  quit   - 서버 종료");
            Console.WriteLine();

            while (isRunning)
            {
                try
                {
                    var input = Console.ReadLine();
                    if (string.IsNullOrEmpty(input)) continue;

                    switch (input.ToLower().Trim())
                    {
                        case "status":
                            gameServer.PrintStatus();
                            break;

                        case "quit":
                        case "exit":
                            isRunning = false;
                            await gameServer.StopAsync();
                            Environment.Exit(0);
                            break;

                        default:
                            Console.WriteLine("알 수 없는 명령어입니다. 'status' 또는 'quit'을 입력하세요.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"명령어 처리 오류: {ex.Message}");
                }
            }
        }
    }
}
