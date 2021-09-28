using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Snakes;
using System.Diagnostics;
using System.Drawing;

try
{
    using (var client = await ConnectClient())
    {
        await DoClientWork(client);
        Console.ReadKey();
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

static async Task DoClientWork(IClusterClient client)
{
    // example of calling grains from the initialized client
    var player = client.GetGrain<IPlayer>("Pilchie");
    var response = await player.Advance();
    Console.WriteLine($"\n\n{response}\n\n");
}

var players = new List<Player>();
var berries = new List<Pixel>();

var random = new Random();
var boardSize = new Size(Console.WindowWidth, Console.WindowHeight - 1);
var self = new Player(random, KnownColor.Blue, KnownColor.DarkBlue, boardSize);
players.Add(self);
berries.Add(new Pixel(random.OnScreen(0, boardSize), KnownColor.Red));

for (int i = 0; i < 4; i++)
{
    players.Add(new Player(random, KnownColor.Green, KnownColor.DarkGreen, boardSize));
    berries.Add(new Pixel(random.OnScreen(0, boardSize), KnownColor.Red));
}

var originalBg = Console.BackgroundColor;
var originalFg = Console.ForegroundColor;
Console.BackgroundColor = ConsoleColor.Black;

while (self.IsAlive && players.Count > 1)
{
    Console.Clear();
    Console.ForegroundColor = ConsoleColor.White;
    Console.SetCursorPosition(0, Console.WindowHeight - 1);
    Console.Write($"Score: {self.Score}");
    foreach (var b in berries)
    {
        DrawPixel(b);
    }

    foreach (var p in players)
    {
        foreach (var px in p.Body)
        {
            DrawPixel(px);
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
                self.TurnLeft();
            }
            else if (key == ConsoleKey.RightArrow)
            {
                self.TurnRight();
            }
        }
    }

    foreach (var p in players.Skip(1))
    {
        var r = random.Next(10);
        if (r == 0)
        {
            p.TurnLeft();
        }
        else if (r == 1)
        {
            p.TurnRight();
        }
    }

    var playersToRemove = new List<Player>();
    var berriesToRemove = new List<Pixel>();

    foreach (var p in players)
    {
        if (!p.Advance())
        {
            playersToRemove.Add(p);
        }
    }

    foreach (var p in players)
    {
        foreach (var b in berries)
        {
            if (p.Head.Location == b.Location)
            {
                p.FoundBerry();
                berriesToRemove.Add(b);
            }
        }

        foreach (var p2 in players)
        {
            foreach (var b in p2.Body.Skip(1))
            {
                if (p.Head.Location == b.Location)
                {
                    playersToRemove.Add(p);
                }
            }
        }
    }

    foreach (var p in playersToRemove)
    {
        players.Remove(p);
        p.IsAlive = false;
    }

    foreach (var b in berriesToRemove)
    {
        berries.Remove(b);
    }

    for (int i = 0; i < players.Count - berries.Count; i++)
    {
        berries.Add(new Pixel(random.OnScreen(border: 0, boardSize), KnownColor.Red));
    }
}

Console.ForegroundColor = ConsoleColor.White;
Console.SetCursorPosition(0, Console.WindowHeight - 1);
Console.WriteLine($"GAME OVER! Your score was: {self.Score}.  You {(self.IsAlive ? "won!" : "lost :'(")}");
Console.BackgroundColor = originalBg;
Console.ForegroundColor = originalFg;

static void DrawPixel(Pixel pixel)
{
    Console.SetCursorPosition(pixel.Location.X, pixel.Location.Y);
    Console.ForegroundColor = MapToConsoleColor(pixel.Color);
    Console.Write("█");
    Console.SetCursorPosition(0, Console.WindowHeight - 1);
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
