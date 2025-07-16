namespace dArtagnan.Server;

class Program
{
    private static TcpServer tcpServer = null!;
    private static AdminConsole adminConsole = null!; // null!는 "나중에 초기화할 것"을 의미

    static async Task Main(string[] args)
    {
        Console.WriteLine("D'Artagnan 게임 서버");
        Console.WriteLine("==================");

        // 종료 시그널 처리
        Console.CancelKeyPress += async (sender, e) =>
        {
            e.Cancel = true;
            adminConsole.Stop();
            await tcpServer.StopAsync();
            Environment.Exit(0);
        };

        // 서버 시작
        int port = 7777;
        tcpServer = new TcpServer();
        var serverTask = tcpServer.StartAsync(port);

        // 관리자 콘솔 초기화
        adminConsole = new AdminConsole(tcpServer);
        _ = Task.Run(() => adminConsole.StartHandlingAsync());

        await serverTask;
    }
}