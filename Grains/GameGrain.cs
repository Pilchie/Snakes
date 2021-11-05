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
    private readonly List<Point> _berries = new();
    private readonly List<IPlayer> _players = new();
    private Size _boardSize;
    private int _currentRound;
    private int _expectedPlayers;
    private GameState _currentState = GameState.NoGame;

    public Task InitializeNewGame(Size boardSize, int expectedPlayers)
    {
        if (_currentState != GameState.NoGame)
        {
            throw new InvalidOperationException($"Can't transition from '{_currentState}' to '{nameof(GameState.Lobby)}'");
        }

        _currentState = GameState.Lobby;
        _currentRound = 0;
        _expectedPlayers = expectedPlayers;
        _boardSize = boardSize;
        _berries.Clear();
        _players.Clear();
        return Task.CompletedTask;
    }

    public Task<Size> GetBoardSize()
        => Task.FromResult(_boardSize);

    public Task<int> GetExpectedPlayers()
        => Task.FromResult(_expectedPlayers);

    public async Task Start()
    {
        if (_currentState != GameState.Lobby)
        {
            throw new InvalidOperationException($"Can't transition from '{_currentState}' to '{nameof(GameState.InProgress)}'");
        }

        if (!_players.Any())
        {
            throw new InvalidOperationException("No human player when starting");
        }

        for (int i = _players.Count; i < _expectedPlayers; i++)
        {
            var p = GrainFactory.GetGrain<IPlayer>($"AI-ControlledPlayer-{i:g}");
            await p.SetHumanControlled(false);
            await p.JoinGame(this);
        }

        for (int _ = 0; _ < _players.Count; _++)
        {
            _berries.Add(Random.Shared.OnScreen(0, _boardSize));
        }

        _currentState = GameState.InProgress;
        var t = Task.Run(async () => await GameLoop(CancellationToken.None));
    }

    public Task<GameState> GetCurrentState()
    {
        return Task.FromResult(_currentState);
    }

    private async Task<bool> IsInProgress()
    {
        var alive = new List<IPlayer>();
        foreach (var p in _players)
        {
            if (await p.IsAlive())
            {
                alive.Add(p);
            }
        }

        if (alive.Count > 1)
        {
            foreach (var p in alive)
            {
                if (await p.IsHumanControlled())
                {
                    return true;
                }
            }
        }

        return false;
    }

    public Task<IEnumerable<Point>> GetBerryPositions()
    {
        return Task.FromResult<IEnumerable<Point>>(_berries);
    }

    public async Task<int> PlayRound(int round)
    {
        while (_currentState == GameState.InProgress && _currentRound == round)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(10));
        }

        return _currentRound;
    }

    private async Task GameLoop(CancellationToken cancellationToken)
    {
        // Give players a couple of seconds to see the starting notice.
        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

        while (!cancellationToken.IsCancellationRequested && _currentState == GameState.InProgress)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);

            await PlayRound();
            _currentRound++;
        }
    }

    private async Task PlayRound()
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

        if (!await IsInProgress())
        {
            _currentState = GameState.NoGame;
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
