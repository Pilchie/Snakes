using Blazor.Extensions;
using Blazor.Extensions.Canvas;
using Blazor.Extensions.Canvas.Canvas2D;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using System.Drawing;

namespace Snakes.Client.Pages;

public partial class Play : IAsyncDisposable
{
    BECanvas? _canvas;
    Canvas2DContext? _context;
    HubConnection? _hubConnection;
    ElementReference _blueSquare;
    ElementReference _darkBlueSquare;
    ElementReference _greenSquare;
    ElementReference _darkGreenSquare;
    ElementReference _orangeSquare;
    ElementReference _darkOrangeSquare;
    ElementReference _redSquare;

    private int _expectedPlayers;
    private int _currentPlayers;
    private GameState _gameState;
    private Size _boardSize;
    private bool _alive = true;
    private int _score;
    private string _id = "";
    private int _currentRound = 0;
    private int _lastRenderedRound = 0;
    IList<PlayerState> _players = new List<PlayerState>();
    IEnumerable<Point> _berries = Array.Empty<Point>();
    private int _width;
    private int _height;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }
        _context = await _canvas.CreateCanvas2DAsync();
        await JSRuntime.InvokeAsync<object>("initGame", DotNetObjectReference.Create(this));

        var hubUrl = NavigationManager.ToAbsoluteUri("/snakehub");
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<int>("OnExpectedPlayerCountChanged", newCount => _expectedPlayers = newCount);
        _hubConnection.On<GameState>("OnStateChanged", async state =>
        {
            switch (state)
            {
                case GameState.NoGame:
                    await OutputTextAsync($"Game over! You {(_alive ? "won" : "lost")}!", clear: false, 100, 200);
                    break;
                case GameState.Lobby:
                    await OutputTextAsync($"Waiting for enough players, currently {_currentPlayers}/{_expectedPlayers}", clear: true, 100, 100);
                    await OutputTextAsync("Press enter to start with NPCs", clear: false, x: 100, y: 200);

                    break;
                case GameState.InProgress:
                    if (_gameState == GameState.Lobby)
                    {
                        await OutputTextAsync($"Game starting...", clear: true, 100, 100);
                    }
                    else if (_gameState == GameState.NoGame)
                    {
                        await OutputTextAsync($"Another game is already in progress, try again later", clear: true, 100, 100);
                    }
                    break;
            }
            _gameState = state;

        });
        _hubConnection.On<int>("OnPlayerJoined", async count =>
        {
            _currentPlayers = count;
            await OutputTextAsync($"Waiting for enough players, now {_currentPlayers}/{_expectedPlayers}", clear: true, 100, 100);
            await OutputTextAsync("Press enter to start with NPCs", clear: false, x: 100, y: 200);

        });
        _hubConnection.On<Size>("OnBoardSizeChanged", size => _boardSize = size);
        _hubConnection.On<IList<PlayerState>, IEnumerable<Point>>("OnNewRound", UpdatePlayerState);

        _hubConnection.On<string>("OnDied", id => _alive = false);
        _hubConnection.On<string, int>("OnScoreChanged", (id, newScore) => _score = newScore);
        await _hubConnection.StartAsync();

        _gameState = await _hubConnection.InvokeAsync<GameState>("GetCurrentState");
        if (_gameState == GameState.NoGame)
        {
            var desiredCount = 5;
            await _hubConnection.InvokeAsync("InitializeNewGame", new Size(96, 24), desiredCount);
            await OutputTextAsync($"Starting new game with {desiredCount} players.", clear: true, 100, 100);
        }
        else if (_gameState == GameState.Lobby)
        {
            var lobbyState = await _hubConnection.InvokeAsync<LobbyState>("GetLobbyState");
            _currentPlayers = lobbyState.CurrentPlayers;
            _expectedPlayers = lobbyState.ExpectedPlayers;
            _boardSize = lobbyState.BoardSize;
            await OutputTextAsync($"Found existing lobby. Waiting for more players, currently {_currentPlayers}/{_expectedPlayers}.", clear: true, 100, 100);
            await OutputTextAsync("Press enter to start with NPCs", clear: false, x: 100, y: 200);
        }
        else
        {
            Logger.LogError("Game in unpected state {gameState}. Try running again.", _gameState);
        }

        var name = "pilchie";
        _id = await _hubConnection.InvokeAsync<string>("JoinGame", name);
        await OutputTextAsync($"Joining game as {name}.", false, 100, 50);
    }

    private async ValueTask OutputTextAsync(string text, bool clear, int x, int y)
    {
        if (_context is null)
        {
            throw new InvalidOperationException($"'{nameof(_context)}' shouldn't be null.");
        }

        if (clear)
        {
            await _context.ClearRectAsync(0, 0, _width, _height);
        }
        await _context.SetStrokeStyleAsync("white");
        await _context.SetFontAsync("24px ver2dana");
        await _context.StrokeTextAsync(text, x, y);
    }

    [JSInvokable]
    public async ValueTask GameLoop(float timeStamp, int screenWidth, int screenHeight)
    {
        await Render();
    }

    private async ValueTask Render()
    {
        if (_context is null)
        {
            throw new InvalidOperationException($"'{nameof(_context)}' shouldn't be null.");
        }

        if (_gameState == GameState.InProgress && _alive)
        {
            if (_currentRound == _lastRenderedRound)
            {
                return;
            }

            _lastRenderedRound = _currentRound;

            await _context.ClearRectAsync(0, 0, _width, _height);

            var xscale = _width / _boardSize.Width;
            var yscale = _height / _boardSize.Height;
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

            await OutputTextAsync($"Score: {_score}", clear: false, 5, 50);
        }
    }

    async Task DrawPlayer(PlayerState player, ElementReference headColor, ElementReference tailColor, int width, int height)
    {
        if (_context is null)
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

    void UpdatePlayerState(IList<PlayerState> players, IEnumerable<Point> berries)
    {
        this._players = players;
        this._berries = berries;
        this._currentRound++;
    }

    [JSInvokable]
    public async ValueTask OnKeyDown(int keyCode)
    {
        if (_hubConnection is null)
        {
            return;
        }

        if (keyCode == (int)Keys.Left)
        {
            await _hubConnection.InvokeAsync("TurnLeft");
        }
        else if (keyCode == (int)Keys.Right)
        {
            await _hubConnection.InvokeAsync("TurnRight");
        }
        else if (keyCode == (int)Keys.Enter)
        {
            if (_gameState == GameState.Lobby)
            {
                await _hubConnection.SendAsync("StartGame");
            }
        }
    }

    [JSInvokable]
    public void OnResize(int width, int height)
    {
        _width = width;
        _height = height;
    }

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
    public async ValueTask DisposeAsync()
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}

public enum Keys
{
    Enter = 13,
    Up = 38,
    Left = 37,
    Down = 40,
    Right = 39,
    Space = 32,
    LeftCtrl = 17,
    LeftAlt = 18,
}
