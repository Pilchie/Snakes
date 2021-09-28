using Orleans;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Drawing;

namespace Snakes;

public interface IPlayer : IGrainWithStringKey
{
    /// <summary>
    /// Advances the player one space in the game.
    /// </summary>
    /// <returns>True if the player is out of bounds, false otherwise</returns>
    Task<bool> Advance();
}

public class Player
{
    private readonly List<Pixel> _body = new List<Pixel>(5);
    private readonly Size _boardSize;
    private Pixel? _last;

    public Pixel Head
    {
        get => _body[0];
        set => _body[0] = value;
    }

    public Player(Random random, KnownColor headColor, KnownColor bodyColor, Size boardSize)
    {
        _boardSize = boardSize;
        Direction = (Direction)random.Next(4);
        _body.Add(new Pixel(random.OnScreen(border: 5, _boardSize), headColor));
        var pixel = Head;
        for (int i = 0; i < 4; i++)
        {
            pixel = new Pixel(pixel.Location.Move(Direction.OppositeOf()), bodyColor);
            _body.Add(pixel);
        }
    }

    public IEnumerable<Pixel> Body
        => _body;

    public Direction Direction { get; private set; }

    public bool IsAlive { get; set; } = true;

    public int Score { get; private set; }

    public bool Advance()
    {
        var bodyColor = _body.Last().Color;
        _last = _body.Last();
        for (int i = _body.Count - 1; i > 0; i--)
        {
            _body[i] = new Pixel(_body[i - 1].Location, bodyColor);
        }

        Head = new Pixel(Head.Location.Move(Direction), Head.Color);

        if (Head.Location.X < 0
            || Head.Location.Y < 0
            || Head.Location.X >= _boardSize.Width
            || Head.Location.Y >= _boardSize.Height)
        {
            return false;
        }

        return true;
    }

    public void TurnLeft()
        => Direction = Direction.LeftOf();

    public void TurnRight()
        => Direction = Direction.RightOf();

    public void FoundBerry()
    {
        Score++;
        if (_last is not null)
        {
            _body.Add(_last);
        }
    }
}
