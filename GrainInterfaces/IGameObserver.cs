using Orleans;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Snakes;

public interface IGameObserver : IGrainObserver
{
   Task OnExpectedPlayerCountChanged(int newCount);
   Task OnStateChanged(GameState newState);
   Task OnPlayerJoined(int newCount);
   Task OnBoardSizeChanged(Size newSize);
   Task OnNewRound();
   Task OnNewRound(IList<PlayerState> players, IEnumerable<Point> berries);

   Task OnDied(string id);
   Task OnScoreChanged(string id, int newScore);
}
