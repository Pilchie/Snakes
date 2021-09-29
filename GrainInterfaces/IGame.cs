using Orleans;
using System.Drawing;
using System.Threading.Tasks;

namespace Snakes;

public interface IGame : IGrainWithGuidKey
{
    Task<Size> GetBoardSize();
    Task SetBoardSize(Size boardSize);
}
