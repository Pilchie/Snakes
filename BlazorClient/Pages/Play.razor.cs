using Blazor.Extensions;
using Blazor.Extensions.Canvas;
using Blazor.Extensions.Canvas.Canvas2D;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace Snakes.Client.Pages;

public partial class Play : IAsyncDisposable
{
    private readonly string _playerName = "Pilchie";

    BECanvas? _canvas;
    Canvas2DContext? _context;
    HubConnection? _hubConnection;

    private LobbyState _lobbyState = new(0, 5, new Size(96, 24));
    private GameState _gameState;
    private bool _alive = true;
    private int _score;
    private string _id = "";
    private int _currentRound = 0;
    private int _lastRenderedRound = 0;
    IList<PlayerState> _players = new List<PlayerState>();
    IEnumerable<Point> _berries = Array.Empty<Point>();
    private Point _mouseCoords = new();
    private int _width;
    private int _height;
    private bool _missedStart;

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

        _hubConnection.On<int>("OnExpectedPlayerCountChanged", newCount => _lobbyState = _lobbyState with { ExpectedPlayers = newCount });
        _hubConnection.On<GameState>("OnStateChanged", async state =>
        {
            _missedStart = false;
            switch (state)
            {
                case GameState.NoGame:
                    await OutputTextAsync($"Game over! You {(_alive ? "won" : "lost")}!", clear: false, 100, 200);
                    await DrawPlayAgain();

                    break;
                case GameState.Lobby:
                    await DrawLobby();
                    break;
                case GameState.InProgress:
                    if (_gameState == GameState.Lobby)
                    {
                        await OutputTextAsync($"Game starting...", clear: true, 100, 100);
                    }
                    else if (_gameState == GameState.NoGame)
                    {
                        _missedStart = true;
                        await OutputTextAsync($"Another game is already in progress, try again later", clear: true, 100, 100);
                        await DrawPlayAgain();
                    }
                    break;
            }
            _gameState = state;

        });
        _hubConnection.On<int>("OnPlayerJoined", async count =>
        {
            _lobbyState = _lobbyState with { CurrentPlayers = count };
            await DrawLobby();

        });
        _hubConnection.On<Size>("OnBoardSizeChanged", async size =>
        {
            _lobbyState = _lobbyState with { BoardSize = size };
            await DrawLobby();
        });
        _hubConnection.On<IList<PlayerState>, IEnumerable<Point>>("OnNewRound", UpdatePlayerState);

        _hubConnection.On<string>("OnDied", id => _alive = false);
        _hubConnection.On<string, int>("OnScoreChanged", (id, newScore) => _score = newScore);
        await _hubConnection.StartAsync();

        _gameState = await _hubConnection.InvokeAsync<GameState>("GetCurrentState");
        if (_gameState == GameState.NoGame)
        {
            await _hubConnection.InvokeAsync("InitializeNewGame", _lobbyState.BoardSize, _lobbyState.ExpectedPlayers); ;
        }
        else if (_gameState == GameState.Lobby)
        {
            _lobbyState = await _hubConnection.InvokeAsync<LobbyState>("GetLobbyState");
            await DrawLobby();
        }
        else
        {
            Logger.LogError("Game in unpected state {gameState}. Try running again.", _gameState);
        }

        _id = await _hubConnection.InvokeAsync<string>("JoinGame", _playerName);
    }

    private async Task DrawPlayAgain()
    {
        if (_context is null)
        {
            throw new InvalidOperationException($"'{nameof(_context)}' shouldn't be null.");
        }

        await _context.BeginBatchAsync();
        try
        {
            var playText = "Play again";
            var playMetrics = await _context.MeasureTextAsync(playText);
            await _context.SetFillStyleAsync("darkgreen");
            await _context.FillRectAsync(_width / 2 - 100, _height / 2 - 50, 200, 100);
            await OutputTextAsync(playText, clear: false, (int)(_width - playMetrics.Width) / 2, _height / 2);
        }
        finally
        {
            await _context.EndBatchAsync();
        }
    }

    private async ValueTask DrawLobby()
    {
        if (_context is null)
        {
            throw new InvalidOperationException($"'{nameof(_context)}' shouldn't be null.");
        }

        await _context.BeginBatchAsync();
        try
        {
            await _context.ClearRectAsync(0, 0, _width, _height);
            await _context.SetFillStyleAsync("darkgreen");
            await _context.FillRectAsync(0, _height - 100, _width, _height);

            await _context.SetFillStyleAsync("white");
            await _context.SetFontAsync("24px ver2dana");

            await _context.FillTextAsync($"Joined ({_lobbyState.BoardSize.Width},{_lobbyState.BoardSize.Height}) game as {_playerName}.", 100, 100);
            await _context.FillTextAsync($"Waiting for players, currently {_lobbyState.CurrentPlayers}/{_lobbyState.ExpectedPlayers}", 100, 150);
            await _context.FillTextAsync($"Press enter or touch below to start with NPCs for remaining players.", 100, 200);

            await _context.FillTextAsync("Instructions:", 100, 300);
            await _context.FillTextAsync("You are blue, other humans are orange, NPCs are green", 150, 350);
            await _context.FillTextAsync("Use left/right keys to change your direction", 150, 400);
            await _context.FillTextAsync("Collect cherries, but don't hit the edge, yourself, or another snake", 150, 450);
            await _context.FillTextAsync("Be the last snake left alive to win!", 150, 500);
            const string startText = "Start Game!";
            var startMetrics = await _context.MeasureTextAsync(startText);
            await _context.FillTextAsync(startText, (int)(_width - startMetrics.Width) / 2, _height - 50);
        }
        finally
        {
            await _context.EndBatchAsync();
        }
    }

    private async ValueTask OutputTextAsync(string text, bool clear, int x, int y)
    {
        if (_context is null)
        {
            throw new InvalidOperationException($"'{nameof(_context)}' shouldn't be null.");
        }

        await _context.BeginBatchAsync();
        try
        {
            if (clear)
            {
                await _context.ClearRectAsync(0, 0, _width, _height);
            }
            await _context.SetFillStyleAsync("white");
            await _context.SetFontAsync("24px ver2dana");
            await _context.FillTextAsync(text, x, y);
        }
        finally
        {
            await _context.EndBatchAsync();
        }
    }

    [JSInvokable]
    public async Task GameLoop()
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

            await _context.BeginBatchAsync();
            try
            {
                await _context.ClearRectAsync(0, 0, _width, _height);

                if (_lobbyState.BoardSize.Width <= 0)
                {
                    throw new Exception($"_boardSize.Width is {_lobbyState.BoardSize.Width}");
                }
                if (_lobbyState.BoardSize.Height <= 0)
                {
                    throw new Exception($"_boardSize.Height is {_lobbyState.BoardSize.Height}");
                }
                var xscale = (_width - 2) / _lobbyState.BoardSize.Width;
                var yscale = (_height - 102) / _lobbyState.BoardSize.Height;

                var extrax = _width - _lobbyState.BoardSize.Width * xscale;
                var extray = _height - 100 - _lobbyState.BoardSize.Height * yscale;

                await _context.SetStrokeStyleAsync("yellow");
                await _context.StrokeRectAsync(extrax / 2, extray / 2, _lobbyState.BoardSize.Width * xscale + 1, _lobbyState.BoardSize.Height * yscale + 1);
                foreach (var p in _players)
                {
                    if (p.Id == _id)
                    {
                        await DrawPlayer(p, "blue", "darkblue", extrax / 2 + 1, extray / 2 + 1, xscale, yscale);
                    }
                    else if (p.HumanControlled)
                    {
                        await DrawPlayer(p, "orange", "darkorange", extrax / 2 + 1, extray / 2 + 1, xscale, yscale);
                    }
                    else
                    {
                        await DrawPlayer(p, "green", "darkgreen", extrax / 2 + 1, extray / 2 + 1, xscale, yscale);
                    }
                }

                foreach (var b in _berries)
                {
                    await _context.SetFillStyleAsync("red");
                    await _context.FillRectAsync(extrax / 2 + 1 + b.X * xscale, extray / 2 + 1 + b.Y * yscale, xscale, yscale);
                }

                await _context.SetFillStyleAsync("purple");
                await _context.FillRectAsync(0, _height - 100, 100, 100);
                await _context.FillRectAsync(_width - 100, _height - 100, 100, 100);

                var leftText = "◀";
                var scoreText = $"{_playerName}'s score: {_score}";
                var rightText = "▶";
                var leftMetrics = await _context.MeasureTextAsync(leftText);
                var scoreMetrics = await _context.MeasureTextAsync(scoreText);
                var rightMetrics = await _context.MeasureTextAsync(rightText);

                await OutputTextAsync(leftText, false, 50 - (int)leftMetrics.Width / 2, _height - 50);
                await OutputTextAsync(scoreText, clear: false, (int)((_width - scoreMetrics.Width) / 2), _height - 50);
                await OutputTextAsync(rightText, false, _width - 50 - (int)rightMetrics.Width / 2, _height - 50);
            }
            finally
            {
                await _context.EndBatchAsync();
            }
        }
    }

    async Task DrawPlayer(PlayerState player, string headColor, string tailColor, int xoffset, int yoffset, int width, int height)
    {
        if (_context is null)
        {
            throw new InvalidOperationException($"'{nameof(_context)}' shouldn't be null.");
        }

        var head = player.Body[0];
        await _context.SetFillStyleAsync(headColor);
        await _context.FillRectAsync(xoffset + head.X * width, yoffset + head.Y * height, width, height);
        await _context.SetFillStyleAsync(tailColor);
        foreach (var b in player.Body.Skip(1))
        {
            await _context.FillRectAsync(xoffset + b.X * width, yoffset + b.Y * height, width, height);
        }
    }

    void UpdatePlayerState(IList<PlayerState> players, IEnumerable<Point> berries)
    {
        this._players = players;
        this._berries = berries;
        this._currentRound++;
    }

    [JSInvokable]
    public async Task OnKeyDown(int keyCode)
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

    [JSInvokable]
    public void OnMouseMove(int mouseX, int mouseY)
    {
        _mouseCoords = new Point(mouseX, mouseY);
    }

    [JSInvokable]
    public async Task OnMouseDown(MouseButton button)
    {
        if (_hubConnection is null || button != MouseButton.Left)
        {
            return;
        }

        if (_mouseCoords.Y > _height - 100 && _mouseCoords.Y < _height)
        {
            if (_mouseCoords.X > 0 && _mouseCoords.X < 100)
            {
                await _hubConnection.InvokeAsync("TurnLeft");
            }
            else if (_mouseCoords.X > _width - 100 && _mouseCoords.X < _width)
            {
                await _hubConnection.InvokeAsync("TurnRight");
            }
            else if (_gameState == GameState.Lobby)
            {
                await _hubConnection.SendAsync("StartGame");
            }
        }
        else if (_gameState == GameState.NoGame ||
            (_gameState == GameState.InProgress && _missedStart))
        {
            if (_mouseCoords.X > _width / 2 - 100 && _mouseCoords.X < _width / 2 + 100 &&
                _mouseCoords.Y > _height / 2 - 50 && _mouseCoords.Y < _height / 2 + 50)
            {
                NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
            }
        }
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

public enum MouseButton
{
    Left = 0,
    Middle = 1,
    Right = 2
}
