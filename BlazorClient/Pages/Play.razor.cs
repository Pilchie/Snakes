using Blazor.Extensions;
using Blazor.Extensions.Canvas;
using Blazor.Extensions.Canvas.Canvas2D;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using System.Drawing;

namespace Snakes.Client.Pages;

public partial class Play
{
    BECanvas? _canvas;
    Canvas2DContext? _context;
    ElementReference _blueSquare;
    ElementReference _darkBlueSquare;
    ElementReference _greenSquare;
    ElementReference _darkGreenSquare;
    ElementReference _orangeSquare;
    ElementReference _darkOrangeSquare;
    ElementReference _redSquare;
    readonly GameTime _gameTime = new();

    private int _expectedPlayers;
    private int _currentPlayers;
    private GameState _gameState;
    private Size _boardSize;
    private bool _alive;
    private int _score;
    private string _id = "";
    IList<PlayerState> _players = new List<PlayerState>();
    IEnumerable<Point> _berries = Array.Empty<Point>();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }
        _context = await _canvas.CreateCanvas2DAsync();
        await JSRuntime.InvokeAsync<object>("initGame", DotNetObjectReference.Create(this));

        var hubUrl = NavigationManager.ToAbsoluteUri("/snakehub");
        var hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        hubConnection.On<int>("OnExpectedPlayerCountChanged", newCount => _expectedPlayers = newCount);
        hubConnection.On<GameState>("OnStateChanged", state =>
        {
            switch (state)
            {
                case GameState.NoGame:
                    break;
                case GameState.Lobby:
                    break;
                case GameState.InProgress:
                    if (_gameState == GameState.Lobby)
                    {
                    }
                    else if (_gameState == GameState.NoGame)
                    {
                    }
                    break;
            }
            _gameState = state;

        });
        hubConnection.On<int>("OnPlayerJoined", count =>
        {
            _currentPlayers = count;
        });
        hubConnection.On<Size>("OnBoardSizeChanged", size => _boardSize = size);
        hubConnection.On<IList<PlayerState>, IEnumerable<Point>>("OnNewRound", DisplayRound);

        hubConnection.On<string>("OnDied", id => _alive = false);
        hubConnection.On<string, int>("OnScoreChanged", (id, newScore) => _score = newScore);
        await hubConnection.StartAsync();

        _gameState = await hubConnection.InvokeAsync<GameState>("GetCurrentState");
        if (_gameState == GameState.NoGame)
        {
            var desiredCount = 5;
            await hubConnection.InvokeAsync("InitializeNewGame", new Size(96, 24), desiredCount);
        }
        else if (_gameState == GameState.Lobby)
        {
            var lobbyState = await hubConnection.InvokeAsync<LobbyState>("GetLobbyState");
            _currentPlayers = lobbyState.CurrentPlayers;
            _expectedPlayers = lobbyState.ExpectedPlayers;
            _boardSize = lobbyState.BoardSize;
        }
        else
        {
            Logger.LogError("Game in unpected state {gameState}. Try running again.", _gameState);
        }

        var name = "pilchie";
        _id = await hubConnection.InvokeAsync<string>("JoinGame", name);
    }

    [JSInvokable]
    public async ValueTask GameLoop(float timeStamp, int screenWidth, int screenHeight)
    {
        _gameTime.TotalTime = timeStamp;
        await Render(screenWidth, screenHeight);
    }

    private async ValueTask Render(int width, int height)
    {
        if (_context == null)
        {
            throw new InvalidOperationException($"'{nameof(_context)}' shouldn't be null.");
        }

        await _context.ClearRectAsync(0, 0, width, height);

        var xscale = width / _boardSize.Width;
        var yscale = height / _boardSize.Height;
        foreach (var p in _players)
        {
            if (p.Id == _id)
            {
                await DrawPlayer(p, _blueSquare, _darkBlueSquare, xscale, yscale);
            }
            else if (p.HumanControlled)
            {
                await DrawPlayer(p, _orangeSquare, _darkOrangeSquare, xscale, yscale);
            }
            else
            {
                await DrawPlayer(p, _greenSquare, _darkGreenSquare, xscale, yscale);
            }
        }

        foreach (var b in _berries)
        {
            await _context.DrawImageAsync(this._redSquare, b.X * xscale, b.Y * yscale, xscale, yscale);
        }

    }

    async Task DrawPlayer(PlayerState player, ElementReference headColor, ElementReference tailColor, int width, int height)
    {
        if (_context == null)
        {
            throw new InvalidOperationException($"'{nameof(_context)}' shouldn't be null.");
        }

        var head = player.Body[0];
        await _context.DrawImageAsync(headColor, head.X * width, head.Y * height, width, height);
        foreach (var b in player.Body.Skip(1))
        {
            await _context.DrawImageAsync(tailColor, b.X * width, b.Y * height, width, height);

        }
    }

    void DisplayRound(IList<PlayerState> players, IEnumerable<Point> berries)
    {
        this._players = players;
        this._berries = berries;
    }
}

public class Sprite
{
    public Size Size { get; set; }
    public ElementReference SpriteSheet { get; set; }
}

public class GameTime
{
    private float _totalTime = 0;

    /// <summary>
    /// total time elapsed since the beginning of the game
    /// </summary>
    public float TotalTime
    {
        get => _totalTime;
        set
        {
            this.ElapsedTime = value - _totalTime;
            _totalTime = value;

        }
    }

    /// <summary>
    /// time elapsed since last frame
    /// </summary>
    public float ElapsedTime { get; private set; }
}
