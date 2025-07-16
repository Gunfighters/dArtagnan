namespace dArtagnan.Server;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("D'Artagnan 게임 서버");
        Console.WriteLine("==================");

        int port = 7777;
        var tcpServer = new TcpServer(port);

        // 무한 대기
        await Task.Delay(-1);
    }
}