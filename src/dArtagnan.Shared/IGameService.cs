
using MagicOnion;

public interface IGameService : IService<IGameService>
{
    // UnaryResult GenerateException(string message);
    // UnaryResult SendReportAsync(string message);
    public UnaryResult<int> SumAsync(int x, int y);
}