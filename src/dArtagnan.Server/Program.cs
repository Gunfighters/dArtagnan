using dArtagnan.Server.Core;

namespace dArtagnan.Server
{
    class Program
    {
        private static readonly GameServer gameServer = new GameServer();
        private static CommandHandler commandHandler = null!; // null!는 "나중에 초기화할 것"을 의미

        static async Task Main(string[] args)
        {
            Console.WriteLine("D'Artagnan 게임 서버");
            Console.WriteLine("==================");

            // 관리자 명령어 핸들러 초기화
            commandHandler = new CommandHandler(gameServer);

            // 종료 시그널 처리
            Console.CancelKeyPress += async (sender, e) =>
            {
                e.Cancel = true;
                commandHandler.Stop();
                await gameServer.StopAsync();
                Environment.Exit(0);
            };

            // 서버 시작
            int port = 7777;
            var serverTask = gameServer.StartAsync(port);

            // 관리자 명령어 처리
            _ = Task.Run(() => commandHandler.StartHandlingAsync());

            await serverTask;
        }
    }
}
