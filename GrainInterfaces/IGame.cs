using Orleans;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace Snakes;

public interface IGame : IGrainWithGuidKey
{
    Task<Size> GetBoardSize();
    Task InitializeNewGame(Size boardSize, int expectedPlayerCount);
    Task<int> GetExpectedPlayers();
    Task Start();
    Task<GameState> GetCurrentState();
    Task<IEnumerable<Point>> GetBerryPositions();
    Task AddPlayer(IPlayer player);
    Task<IEnumerable<IPlayer>> GetPlayers();
    Task<int> GetCurrentRound();
}
