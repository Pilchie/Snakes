using Orleans;
using System.Threading.Tasks;

namespace Snakes
{
    public interface IPlayer : IGrainWithStringKey
    {
        /// <summary>
        /// Advances the player one space in the game.
        /// </summary>
        /// <returns>True if the player is out of bounds, false otherwise</returns>
        Task<bool> Advance();
    }
}
