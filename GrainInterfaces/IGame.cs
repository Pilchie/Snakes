using Orleans;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace Snakes;

public interface IGame : IGrainWithGuidKey
{
    Task<Size> GetBoardSize();
    Task InitializeNewGame(Size boardSize);
    Task Start();
    Task<bool> IsInProgress();
    Task<IEnumerable<Point>> GetBerryPositions();
    Task AddPlayer(IPlayer player);
    Task<IEnumerable<IPlayer>> GetPlayers();
    Task PlayRound();
}
