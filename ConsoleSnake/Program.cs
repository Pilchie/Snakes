using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Snakes;
using Spectre.Console;
using System.Diagnostics;
using System.Drawing;

using Color = Spectre.Console.Color;

;
var originalBg = AnsiConsole.Background;
var originalFg = AnsiConsole.Foreground;
AnsiConsole.Background = Color.Black;

try
{
    using (var client = await ConnectClient())
    {
        var game = client.GetGrain<IGame>(Guid.Empty);
        var boardSize = new Size(AnsiConsole.Profile.Width - 2, AnsiConsole.Profile.Height - 3);
        await game.InitializeNewGame(boardSize);

        var self = client.GetGrain<IPlayer>("Pilchie");
        await self.SetHumanControlled(true);
        await self.JoinGame(game);
        await game.Start();

        while (await self.IsAlive() && await game.IsInProgress())
        {
            var canvas = new Canvas(boardSize.Width + 2, boardSize.Height + 2);
            DrawBorder(canvas);

            canvas.PixelWidth = 1;

            foreach (var b in await game.GetBerryPositions())
            {
                canvas.SetPixel(b.X + 1, b.Y + 1, Color.Red);
            }

            var players = await game.GetPlayers();
            foreach (var p in players)
            {
                if (p.GetPrimaryKeyString() == self.GetPrimaryKeyString())
                {
                    await DrawPlayer(canvas, p, Color.Blue, Color.DarkBlue);
                }
                else if (await p.IsHumanControlled())
                {
                    await DrawPlayer(canvas, p, Color.Orange1, Color.DarkOrange);
                }
                else
                {
                    await DrawPlayer(canvas, p, Color.Green, Color.DarkGreen);
                }
            }

            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < 200)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    if (key == ConsoleKey.LeftArrow)
                    {
                        await self.TurnLeft();
                    }
                    else if (key == ConsoleKey.RightArrow)
                    {
                        await self.TurnRight();
                    }
                }
            }

            await game.PlayRound();

            AnsiConsole.Cursor.SetPosition(0, 0);
            AnsiConsole.Write(canvas);
            AnsiConsole.Cursor.SetPosition(1, boardSize.Height + 2);
            AnsiConsole.Markup($"[black on white]Score: {await self.GetScore()}[/]");
            AnsiConsole.Cursor.SetPosition(0, 0);
        }

        AnsiConsole.Cursor.SetPosition(5, boardSize.Height - 5);
        AnsiConsole.MarkupLine($"[bold yellow on blue]GAME OVER! Your score was: {await self.GetScore()}.  You {(await self.IsAlive() ? "won!" : "lost :'(")}[/]");
    }
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

static async Task<IClusterClient> ConnectClient()
{
    IClusterClient client;
    client = new ClientBuilder()
        .UseLocalhostClustering()
        .Configure<ClusterOptions>(options =>
        {
            options.ClusterId = "dev";
            options.ServiceId = "Snakes";
        })
        .ConfigureLogging(logging => logging.AddConsole())
        .Build();

    await client.Connect();
    AnsiConsole.WriteLine("Client successfully connected to silo host \n");
    return client;
}

static async Task DrawPlayer(Canvas canvas, IPlayer player, Color headColor, Color tailColor)
{
    var body = await player.GetBody();
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
