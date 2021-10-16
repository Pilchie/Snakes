using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Snakes;
using Spectre.Console;
using System.Diagnostics;
using System.Drawing;

using Color = Spectre.Console.Color;

var originalBg = Console.BackgroundColor;
var originalFg = Console.ForegroundColor;

try
{
    using (var client = await ConnectClient())
    {
        var players = new List<IPlayer>(5);
        var berries = new List<Point>(5);

        var game = client.GetGrain<IGame>(Guid.Empty);
        var boardSize = new Size(Console.WindowWidth - 2, Console.WindowHeight - 3);
        await game.SetBoardSize(boardSize);

        var self = client.GetGrain<IPlayer>("Pilchie");
        players.Add(self);
        berries.Add(Random.Shared.OnScreen(0, boardSize));

        for (int i = 0; i < 4; i++)
        {
            players.Add(client.GetGrain<IPlayer>(i.ToString("g")));
            berries.Add(Random.Shared.OnScreen(0, boardSize));
        }

        await Task.WhenAll(players.Select(p => p.JoinGame(game)));

        AnsiConsole.Background = Color.Black;

        while (await self.IsAlive() && players.Count > 1)
        {
            var canvas = new Canvas(boardSize.Width + 2, boardSize.Height + 2);
            DrawBorder(canvas);

            canvas.PixelWidth = 1;

            foreach (var b in berries)
            {
                canvas.SetPixel(b.X + 1, b.Y + 1, Color.Red);
            }

            await DrawPlayer(canvas, self, Color.Blue, Color.DarkBlue);
            foreach (var p in players.Skip(1))
            {
                await DrawPlayer(canvas, p, Color.Green, Color.DarkGreen);
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

            foreach (var p in players.Skip(1))
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

            var playersToRemove = new List<IPlayer>();
            var berriesToRemove = new List<Point>();

            foreach (var p in players)
            {
                if (!(await p.Advance()))
                {
                    playersToRemove.Add(p);
                }
            }

            foreach (var p in players)
            {
                var head = await p.GetHead();
                foreach (var b in berries)
                {
                    if (head == b)
                    {
                        await p.FoundBerry();
                        berriesToRemove.Add(b);
                    }
                }

                foreach (var p2 in players)
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
                players.Remove(p);
                await p.Die();
            }

            foreach (var b in berriesToRemove)
            {
                berries.Remove(b);
            }

            for (int i = 0; i < players.Count - berries.Count; i++)
            {
                berries.Add(Random.Shared.OnScreen(border: 0, boardSize));
            }

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
    Console.WriteLine($"\nException while trying to run client: {e.Message}");
    Console.WriteLine("Make sure the silo the client is trying to connect to is running.");
    Console.WriteLine("\nPress any key to exit.");
    Console.ReadKey();
    return;
}
finally
{
    Console.BackgroundColor = originalBg;
    Console.ForegroundColor = originalFg;
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
    Console.WriteLine("Client successfully connected to silo host \n");
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
