using Microsoft.AspNetCore.SignalR.Client;
using Snakes;
using Spectre.Console;
using System.Collections.Generic;
using System.Drawing;

using Color = Spectre.Console.Color;

;
var originalBg = AnsiConsole.Background;
var originalFg = AnsiConsole.Foreground;
AnsiConsole.Background = Color.Black;

var gameState = GameState.NoGame;
var currentPlayers = 0;
var expectedPlayers = 0;
var boardSize = Size.Empty;
var alive = true;
var score = 0;
var id = "";
try
{
    var hubConnection = new HubConnectionBuilder()
            .WithUrl("https://localhost:7193/snakehub")
            .Build();

    hubConnection.On<int>("OnExpectedPlayerCountChanged", newCount => expectedPlayers = newCount);
    hubConnection.On<GameState>("OnStateChanged", state =>
    {
        switch (state)
        {
            case GameState.NoGame:
                AnsiConsole.WriteLine("Ended");
                break;
            case GameState.Lobby:
                AnsiConsole.WriteLine("Waiting for more players to join");

                break;
            case GameState.InProgress:
                if (gameState == GameState.Lobby)
                {
                    AnsiConsole.WriteLine("Starting");
                }
                else if (gameState == GameState.NoGame)
                {
                    AnsiConsole.WriteLine("Game in progress, waiting for it to end");
                }
                break;
        }
        gameState = state;

    });
    hubConnection.On<int>("OnPlayerJoined", count =>
    {
        currentPlayers = count;
        AnsiConsole.WriteLine($"Now {count} players");
    });
    hubConnection.On<Size>("OnBoardSizeChanged", size => boardSize = size);
    hubConnection.On<IList<PlayerState>, IEnumerable<Point>>("OnNewRound", DisplayRound);

    hubConnection.On<string>("OnDied", id => alive = false);
    hubConnection.On<string, int>("OnScoreChanged", (id, newScore) => score = newScore);
    await hubConnection.StartAsync();
    AnsiConsole.Clear();
    AnsiConsole.WriteLine("Welcome to snekz!");
    AnsiConsole.WriteLine();

    var name = AnsiConsole.Ask<string>("What's your name?");

    gameState = await hubConnection.InvokeAsync<GameState>("GetCurrentState");
    if (gameState == GameState.NoGame)
    {
        var desiredCount = AnsiConsole.Ask<int>("How many players should there be (including NPCs)?");
        await hubConnection.InvokeAsync("InitializeNewGame", new Size(96, 24), desiredCount);
        id = await hubConnection.InvokeAsync<string>("JoinGame", name);
    }
    else if (gameState == GameState.Lobby)
    {
        var lobbyState = await hubConnection.InvokeAsync<LobbyState>("GetLobbyState");
        currentPlayers = lobbyState.CurrentPlayers;
        expectedPlayers = lobbyState.ExpectedPlayers;
        boardSize = lobbyState.BoardSize;
        id = await hubConnection.InvokeAsync<string>("JoinGame", name);
    }
    else
    {
        AnsiConsole.WriteLine($"Game in unpected state {gameState}. Try running again.");
        return;
    }

    AnsiConsole.WriteLine("Waiting for other players to join (press any key to start with NPCs for remaining)...");
    var prevCount = 0;
    while (prevCount < expectedPlayers)
    {
        if (currentPlayers != prevCount)
        {
            AnsiConsole.WriteLine($"\tNow {currentPlayers}/{expectedPlayers} players");
            prevCount = currentPlayers;
        }

        await Task.Delay(TimeSpan.FromMilliseconds(200));
        if (Console.KeyAvailable)
        {
            Console.ReadKey();
            await hubConnection.SendAsync("StartGame");
        }
    }

    while (true)
    {
        if (gameState != GameState.InProgress || !alive)
        {
            break;
        }

        if (Console.KeyAvailable)
        {
            var key = Console.ReadKey(true).Key;
            if (key == ConsoleKey.LeftArrow)
            {
                await hubConnection.InvokeAsync("TurnLeft");
            }
            else if (key == ConsoleKey.RightArrow)
            {
                await hubConnection.InvokeAsync("TurnRight");
            }
        }

        await Task.Delay(TimeSpan.FromMilliseconds(20));
    }

    AnsiConsole.Cursor.SetPosition(5, boardSize.Height - 5);
    AnsiConsole.MarkupLine($"[bold yellow on blue]GAME OVER! Your score was: {score}.  You {(alive ? "won!" : "lost :'(")}[/]");
}
catch (Exception e)
{
    AnsiConsole.WriteLine($"\nException while trying to run client: {e.Message}");
    AnsiConsole.WriteLine("Make sure the silo the client is trying to connect to is running.");
    AnsiConsole.WriteLine("\nPress any key to exit.");
    Console.ReadKey();
    return;
}
finally
{
    AnsiConsole.Background = originalBg;
    AnsiConsole.Foreground = originalFg;
}

void DisplayRound(IList<PlayerState> players, IEnumerable<Point> berries)
{
    var canvas = new Canvas(boardSize.Width + 2, boardSize.Height + 2);
    DrawBorder(canvas);

    canvas.PixelWidth = 1;

    foreach (var b in berries)
    {
        canvas.SetPixel(b.X + 1, b.Y + 1, Color.Red);
    }

    foreach (var p in players)
    {
        if (p.Id == id)
        {
            DrawPlayer(canvas, p, Color.Blue, Color.DarkBlue);
        }
        else if (p.HumanControlled)
        {
            DrawPlayer(canvas, p, Color.Orange1, Color.DarkOrange);
        }
        else
        {
            DrawPlayer(canvas, p, Color.Green, Color.DarkGreen);
        }
    }

    AnsiConsole.Cursor.SetPosition(0, 0);
    AnsiConsole.Write(canvas);
    AnsiConsole.Cursor.SetPosition(1, boardSize.Height + 2);
    AnsiConsole.Markup($"[black on white]Score: {score}[/]");
    AnsiConsole.Cursor.SetPosition(0, 0);
}

static void DrawPlayer(Canvas canvas, PlayerState player, Color headColor, Color tailColor)
{
    var body = player.Body;
    var head = body.First();
    canvas.SetPixel(head.X + 1, head.Y + 1, headColor);

    foreach (var px in body.Skip(1))
    {
        canvas.SetPixel(px.X + 1, px.Y + 1, tailColor);
    }
}

static void DrawBorder(Canvas canvas)
{
    for (int x = 0; x < canvas.Width; x++)
    {
        canvas.SetPixel(x, 0, Color.White);
        canvas.SetPixel(x, canvas.Height - 1, Color.White);
    }

    for (int y = 1; y < canvas.Height - 1; y++)
    {
        canvas.SetPixel(0, y, Color.White);
        canvas.SetPixel(canvas.Width - 1, y, Color.White);
    }
}
