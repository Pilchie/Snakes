﻿using Microsoft.Extensions.Logging;
using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Snakes;

public class PlayerGrain : Grain, IPlayer
{
    private readonly List<Point> _body = new(5);
    private Size _boardSize = new();
    private Point? _last;
    private int _score;
    private bool _isAlive = true;
    private bool _humanControlled;
    private Direction _direction;
    private string _name = string.Empty;

    public async Task JoinGame(IGame game)
    {
        _body.Clear();
        _last = null;
        _isAlive = true;

        _boardSize = await game.GetBoardSize();
        _direction = (Direction)Random.Shared.Next(4);
        _body.Add(Random.Shared.OnScreen(border: 5, _boardSize));
        var prev = Head;
        for (int i = 0; i < 4; i++)
        {
            prev = prev.Move(_direction.OppositeOf());
            _body.Add(prev);
        }

        await game.AddPlayer(this);
    }

    /// <summary>
    /// Advances the player one space in the game.
    /// </summary>
    /// <returns>true if the player is still in bounds, false otherwise</returns>
    public Task<bool> Advance()
    {
        _last = _body.Last();
        for (int i = _body.Count - 1; i > 0; i--)
        {
            _body[i] = _body[i - 1];
        }

        Head = Head.Move(_direction);

        return Head.X < 0 || Head.Y < 0 || Head.X >= _boardSize.Width || Head.Y >= _boardSize.Height
            ? Task.FromResult(false)
            : Task.FromResult(true);
    }

    private Point Head
    {
        get => _body[0];
        set => _body[0] = value;
    }

    public Task<Point> GetHead()
        => Task.FromResult(Head);

    public Task Die()
    {
        _isAlive = false;
        return Task.CompletedTask;
    }

    public Task<bool> IsAlive()
        => Task.FromResult(_isAlive);

    public Task<int> GetScore()
        => Task.FromResult(_score);

    public Task TurnLeft()
    {
        _direction = _direction.LeftOf();
        return Task.CompletedTask;
    }

    public Task TurnRight()
    {
        _direction = _direction.RightOf();
        return Task.CompletedTask;
    }

    public Task FoundBerry()
    {
        _score++;
        if (_last is not null)
        {
            _body.Add(_last);
        }
        return Task.CompletedTask;
    }

    public Task<IList<Point>> GetBody()
        => Task.FromResult<IList<Point>>(_body);

    public Task<bool> IsHumanControlled()
        => Task.FromResult(_humanControlled);

    public Task SetHumanControlled(bool humanControlled)
    {
        _humanControlled = humanControlled;
        return Task.CompletedTask;
    }

    public Task<string> GetName()
        => Task.FromResult(_name);

    public Task SetName(string name)
    {
        _name = name;
        return Task.CompletedTask;
    }
}
