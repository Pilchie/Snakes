using Orleans;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Linq;

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
    private readonly int _boardWidth;
    private readonly int _boardHeight;
    private Pixel? _last;

    public Pixel Head
    {
        get => _body[0];
        set => _body[0] = value;
    }

    public Player(Random random, Color headColor, Color bodyColor, int boardWidth, int boardHeight)
    {
        _boardWidth = boardWidth;
        _boardHeight = boardHeight;
        Direction = (Direction)random.Next(4);
        _body.Add(new Pixel(random.OnScreen(border: 5, _boardWidth, _boardHeight), headColor));
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
            || Head.Location.X >= _boardWidth
            || Head.Location.Y >= _boardHeight)
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
