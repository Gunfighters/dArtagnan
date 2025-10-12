namespace dArtagnan.Server;
class Program
{
    public static bool DEV_MODE = true;     //도커로 실행할 때는 false, dotnet run 일 때는 true
    static async Task Main(string[] args)
    {
        Logger.log("[Server][System] D'Artagnan 게임 서버 시작");
        Logger.log("[Server][System] ==================");

        int port = GetPort(args);
        var tcpServer = new TcpServer(port);

        // 무한 대기
        await Task.Delay(-1);
    }

    static int GetPort(string[] args)
    {
        // 1) --port=XXXX 또는 -p XXXX 처리
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.StartsWith("--port="))
            {
                if (int.TryParse(arg.Substring("--port=".Length), out var p)) return p;
            }
            else if (arg == "--port" || arg == "-p")
            {
                if (i + 1 < args.Length && int.TryParse(args[i + 1], out var p)) return p;
            }
        }

        // 2) 환경변수 PORT (도커로 실행할 때)
        var envPort = Environment.GetEnvironmentVariable("PORT");
        if (!string.IsNullOrWhiteSpace(envPort) && int.TryParse(envPort, out var portFromEnv))
        {
            DEV_MODE = false;
            return portFromEnv;
        }

        // 3) 기본값
        return 7777;
    }
}