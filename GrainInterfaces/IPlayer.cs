using Orleans;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace Snakes;

public interface IPlayer : IGrainWithStringKey
{
    /// <summary>
    /// Advances the player one space in the game.
    /// </summary>
    /// <returns>True if the player is out of bounds, false otherwise</returns>
    Task<bool> Advance();

    Task<int> GetScore();

    Task JoinGame(IGame game);

    Task Die();
    Task<bool> IsAlive();

    Task TurnLeft();
    Task TurnRight();

    Task FoundBerry();

    Task<IList<Point>> GetBody();

    Task<Point> GetHead();

    Task<bool> IsHumanControlled();

    Task SetHumanControlled(bool humanControlled);

    Task SetName(string name);
    Task<string> GetName();
}
