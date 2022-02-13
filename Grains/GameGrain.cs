using Microsoft.Extensions.Logging;
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
    private readonly ObserverManager<IGameObserver> _subscriptionManager;
    private readonly List<Point> _berries = new();
    private readonly List<IPlayer> _players = new();
    private Size _boardSize;
    private int _expectedPlayers;
    private GameState _currentState = GameState.NoGame;
    private IDisposable? _timerHandle;

    public GameGrain(ILoggerFactory loggerFactory)
    {
        _subscriptionManager = new ObserverManager<IGameObserver>(TimeSpan.FromMinutes(1), loggerFactory, "TurnSubscriptions");
    }

    public async Task InitializeNewGame(Size boardSize, int expectedPlayers)
    {
        if (_currentState != GameState.NoGame)
        {
            throw new InvalidOperationException($"Can't transition from '{_currentState}' to '{nameof(GameState.Lobby)}'");
        }

        _berries.Clear();
        _players.Clear();
        _expectedPlayers = expectedPlayers;
        await _subscriptionManager.Notify(go => go.OnExpectedPlayerCountChanged(expectedPlayers));
        _boardSize = boardSize;
        await _subscriptionManager.Notify(go => go.OnBoardSizeChanged(boardSize));
        await SetState(GameState.Lobby);
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

        await SetState(GameState.InProgress);

        _timerHandle = RegisterTimer(PlayRound, state: null, dueTime: TimeSpan.FromSeconds(2), period: TimeSpan.FromMilliseconds(200));
    }

    public Task<GameState> GetCurrentState()
    {
        return Task.FromResult(_currentState);
    }

    private async Task SetState(GameState state)
    {
        if (_currentState != state)
        {
            _currentState = state;
            await _subscriptionManager.Notify(go => go.OnStateChanged(state));
        }
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

    private async Task PlayRound(object state)
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
                    var newScore = await p.GetScore();
                    await _subscriptionManager.Notify(go => go.OnScoreChanged(p.GetPrimaryKeyString(), newScore));
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
            await _subscriptionManager.Notify(go => go.OnDied(p.GetPrimaryKeyString()));
        }

        foreach (var b in berriesToRemove)
        {
            _berries.Remove(b);
        }

        for (int i = 0; i < _players.Count - _berries.Count; i++)
        {
            _berries.Add(Random.Shared.OnScreen(border: 0, _boardSize));
        }

        await _subscriptionManager.Notify(go => go.OnNewRound());

        if (!await IsInProgress())
        {
            await SetState(GameState.NoGame);
            _timerHandle?.Dispose();
        }
    }

    public async Task AddPlayer(IPlayer player)
    {
        this._players.Add(player);
        await this._subscriptionManager.Notify(go => go.OnPlayerJoined(this._players.Count));
    }

    public Task<IEnumerable<IPlayer>> GetPlayers()
    {
        return Task.FromResult<IEnumerable<IPlayer>>(_players);
    }

    public Task Subscribe(IGameObserver gameObserver)
    {
        _subscriptionManager.Subscribe(gameObserver, gameObserver);
        return Task.CompletedTask;
    }

    public Task Unsubscribe(IGameObserver gameObserver)
    {
        _subscriptionManager.Unsubscribe(gameObserver);
        return Task.CompletedTask;
    }
}
