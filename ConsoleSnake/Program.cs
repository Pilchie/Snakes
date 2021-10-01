using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Snakes;
using System.Diagnostics;
using System.Drawing;

var originalBg = Console.BackgroundColor;
var originalFg = Console.ForegroundColor;

try
{
    using (var client = await ConnectClient())
    {
        var players = new List<IPlayer>(5);
        var berries = new List<Point>(5);

        var game = client.GetGrain<IGame>(Guid.Empty);
        var boardSize = new Size(Console.WindowWidth - 2, Console.WindowHeight - 2);
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

        Console.BackgroundColor = ConsoleColor.Black;

        while (await self.IsAlive() && players.Count > 1)
        {
            Console.Clear();
            DrawBorder(boardSize);
            DrawAt(new Point(0, boardSize.Height), KnownColor.White, $"Score: {await self.GetScore()}", boardSize);

            foreach (var b in berries)
            {
                DrawPixel(b, KnownColor.Red, boardSize);
            }

            await DrawPlayer(self, KnownColor.Blue, KnownColor.DarkBlue, boardSize);
            foreach (var p in players.Skip(1))
            {
                await DrawPlayer(p, KnownColor.Green, KnownColor.DarkGreen, boardSize);
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
        }

        DrawAt(new Point(5, boardSize.Height - 5), KnownColor.White, $"GAME OVER! Your score was: {await self.GetScore()}.  You {(await self.IsAlive() ? "won!" : "lost :'(")}", boardSize);
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

static async Task DrawPlayer(IPlayer player, KnownColor headColor, KnownColor tailColor, Size boardSize)
{
    var body = await player.GetBody();
    DrawPixel(body.First(), headColor, boardSize);

    foreach (var px in body.Skip(1))
    {
        DrawPixel(px, tailColor, boardSize);
    }
}

static void DrawPixel(Point location, KnownColor color, Size boardSize)
    => DrawAt(location, color, "█", boardSize);

static void DrawAt(Point location, KnownColor color, string value, Size boardSize)
{
    Console.SetCursorPosition(location.X + 1, location.Y + 1);
    Console.ForegroundColor = MapToConsoleColor(color);
    Console.Write(value);
    Console.SetCursorPosition(0, boardSize.Height);
}

static void DrawBorder(Size boardSize)
{
    for (int x = -1; x < boardSize.Width + 1; x++)
    {
        DrawPixel(new Point(x, -1), KnownColor.White, boardSize);
        //DrawPixel(new Point(x, boardSize.Height), KnownColor.White, boardSize);
    }

    for (int y = 0; y < boardSize.Height; y++)
    {
        DrawPixel(new Point(-1, y), KnownColor.White, boardSize);
        DrawPixel(new Point(boardSize.Width, y), KnownColor.White, boardSize);
    }
}

static ConsoleColor MapToConsoleColor(KnownColor color)
    => color switch
    {
        KnownColor.Black => ConsoleColor.Black,
        KnownColor.White => ConsoleColor.White,
        KnownColor.Red => ConsoleColor.Red,
        KnownColor.Blue => ConsoleColor.Blue,
        KnownColor.Green => ConsoleColor.Green,
        KnownColor.DarkBlue => ConsoleColor.DarkBlue,
        KnownColor.DarkGreen => ConsoleColor.DarkGreen,
        _ => throw new NotSupportedException("Unexpected color to draw!"),
    };
