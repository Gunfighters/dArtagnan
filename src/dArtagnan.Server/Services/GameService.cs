using MagicOnion;
using MagicOnion.Server;

namespace dArtagnan.Server.Services
{
    public class GameService : ServiceBase<IGameService>, IGameService
    {
        public async UnaryResult<int> SumAsync(int x, int y)
        {
            Console.WriteLine($"Received: {x}, {y}");
            return x + y;
        }
    }
}