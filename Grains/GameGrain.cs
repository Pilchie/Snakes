using Orleans;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Snakes;

public class GameGrain : Grain, IGame
{
    private Size _boardSize;

    public Task SetBoardSize(Size boardSize)
    {
        _boardSize = boardSize;
        return Task.CompletedTask;
    }

    public Task<Size> GetBoardSize()
        => Task.FromResult(new Size(Console.WindowWidth, Console.WindowHeight));
}
