using Orleans;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Snakes;

public class GameGrain : Grain, IGame
{
    private readonly List<Point> _berries = new List<Point>();
    private readonly List<IPlayer> _players = new List<IPlayer>();
    private Size _boardSize;

    public Task InitializeNewGame(Size boardSize)
    {
        _boardSize = boardSize;
        _berries.Clear();
        _players.Clear();
        return Task.CompletedTask;
    }

    public Task<Size> GetBoardSize()
        => Task.FromResult(_boardSize);

    public async Task Start()
    {
        if (_players.Count != 1)
        {
            throw new InvalidOperationException("No human player when starting");
        }

        for (int i = 0; i < 4; i++)
        {
            var p = GrainFactory.GetGrain<IPlayer>(i.ToString("g"));
            await p.SetHumanControlled(false);
            await p.JoinGame(this);
        }

        for (int _ = 0; _ < _players.Count; _++)
        {
            _berries.Add(Random.Shared.OnScreen(0, _boardSize));
        }
    }

    public async Task<bool> IsInProgress()
    {
        var aliveTasks = _players.Select(p => p.IsAlive());
        var count = 0;
        await foreach (var t in aliveTasks.InOrderOfCompletion())
        {
            count += await t ? 1 : 0;
            if (count > 1)
            {
                return true;
            }
        }

        return false;
    }

    public Task<IEnumerable<Point>> GetBerryPositions()
    {
        return Task.FromResult<IEnumerable<Point>>(_berries);
    }

    public async Task PlayRound()
    {
        foreach (var p in _players)
        {
            if (!await p.IsHumanControlled())
            {
                var r = Random.Shared.Next(10);
                if (r == 0)
                {
                    await p.TurnLeft();
                }
                else if (r == 1)
                {
                    await p.TurnRight();
                }
            }
        }

        var playersToRemove = new List<IPlayer>();
        var berriesToRemove = new List<Point>();

        foreach (var p in _players)
        {
            if (!(await p.Advance()))
            {
                playersToRemove.Add(p);
            }
        }

        foreach (var p in _players)
        {
            var head = await p.GetHead();
            foreach (var b in _berries)
            {
                if (head == b)
                {
                    await p.FoundBerry();
                    berriesToRemove.Add(b);
                }
            }

            foreach (var p2 in _players)
            {
                var p2Body = await p2.GetBody();
                if (p != p2)
                {
                    if (head == p2Body[0])
                    {
                        playersToRemove.Add(p);
                    }
                }

                foreach (var b in p2Body.Skip(1))
                {
                    if (head == b)
                    {
                        playersToRemove.Add(p);
                    }
                }
            }
        }

        foreach (var p in playersToRemove)
        {
            _players.Remove(p);
            await p.Die();
        }

        foreach (var b in berriesToRemove)
        {
            _berries.Remove(b);
        }

        for (int i = 0; i < _players.Count - _berries.Count; i++)
        {
            _berries.Add(Random.Shared.OnScreen(border: 0, _boardSize));
        }
    }

    public Task AddPlayer(IPlayer player)
    {
        this._players.Add(player);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<IPlayer>> GetPlayers()
    {
        return Task.FromResult<IEnumerable<IPlayer>>(_players);
    }
}
