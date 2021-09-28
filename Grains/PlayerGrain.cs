using Orleans;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Snakes;

/// <summary>
/// Advances the player one space in the game.
/// </summary>
/// <returns>True if the player is out of bounds, false otherwise</returns>
public class PlayerGrain : Grain, IPlayer
{
    private readonly ILogger<PlayerGrain> _logger;

    public PlayerGrain(ILogger<PlayerGrain> logger)
    {
        _logger = logger;
    }
    public Task<bool> Advance()
    {
        _logger.LogInformation("Player '{Identity}' Advance()", this.IdentityString);
        return Task.FromResult(false);
    }
}
