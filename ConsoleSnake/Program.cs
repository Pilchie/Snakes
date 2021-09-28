using System.Diagnostics;
using System.Drawing;

var players = new List<Player>();
var berries = new List<Pixel>();

var random = new Random();
var self = new Player(random, ConsoleColor.Blue, ConsoleColor.DarkBlue);
players.Add(self);
berries.Add(new Pixel(random.OnScreen(0), ConsoleColor.Red));

for (int i = 0; i < 4; i++)
{
    players.Add(new Player(random, ConsoleColor.Green, ConsoleColor.DarkGreen));
    berries.Add(new Pixel(random.OnScreen(0), ConsoleColor.Red));
}


var originalBg = Console.BackgroundColor;
var originalFg = Console.ForegroundColor;
Console.BackgroundColor = ConsoleColor.Black;

while (self.IsAlive && players.Count > 1)
{
    Console.Clear();
    foreach (var b in berries)
    {
        b.Draw();
    }

    foreach (var p in players)
    {
        p.Draw();
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
        berries.Add(new Pixel(random.OnScreen(border: 0), ConsoleColor.Red));
    }
}

Console.BackgroundColor = originalBg;
Console.ForegroundColor = originalFg;
Console.Clear();
Console.WriteLine($"GAME OVER! Your score was: {self.Score}.  You {(self.IsAlive ? "won!" : "lost :'(")}");

public static class Extensions
{
    public static Point OnScreen(this Random random, int border)
        => new Point(random.Next(border, Console.WindowWidth - border), random.Next(border, Console.WindowHeight - border));

    public static Point Move(this Point point, Direction direction)
        => direction switch
        {
            Direction.Up => new Point(point.X, point.Y - 1),
            Direction.Left => new Point(point.X - 1, point.Y),
            Direction.Down => new Point(point.X, point.Y + 1),
            Direction.Right => new Point(point.X + 1, point.Y),
            _ => throw new InvalidOperationException("What direction are you asking to move?"),
        };

    public static Direction LeftOf(this Direction direction)
        => direction switch
        {
            Direction.Up => Direction.Left,
            Direction.Left => Direction.Down,
            Direction.Down => Direction.Right,
            Direction.Right => Direction.Up,
            _ => throw new InvalidOperationException("What direction are we going???"),
        };

    public static Direction RightOf(this Direction direction)
        => direction switch
        {
            Direction.Up => Direction.Right,
            Direction.Right => Direction.Down,
            Direction.Down => Direction.Left,
            Direction.Left => Direction.Up,
            _ => throw new InvalidOperationException("What direction are we going???"),
        };

    public static Direction OppositeOf(this Direction direction)
        => direction switch
        {
            Direction.Up => Direction.Down,
            Direction.Right => Direction.Left,
            Direction.Down => Direction.Up,
            Direction.Left => Direction.Right,
            _ => throw new InvalidOperationException("What direction are we going???"),
        };
}

public class Player
{
    private readonly List<Pixel> _body = new List<Pixel>(1);
    private Pixel? _last;

    public Pixel Head
    {
        get => _body[0];
        set => _body[0] = value;
    }

    public Player(Random random, ConsoleColor headColor, ConsoleColor bodyColor)
    {
        Direction = (Direction)random.Next(4);
        _body.Add(new Pixel(random.OnScreen(border: 5), headColor));
        var pixel = Head;
        for (int i = 0; i < 4; i++)
        {
            pixel = new Pixel(pixel.Location.Move(Direction.OppositeOf()), bodyColor);
            _body.Add(pixel);
        }
    }

    public IEnumerable<Pixel> Body
        => _body;

    public Direction Direction { get; private set; }

    public bool IsAlive { get; set; } = true;

    public int Score { get; private set; }

    public bool Advance()
    {
        var bodyColor = _body.Last().Color;
        _last = _body.Last();
        for (int i = _body.Count - 1; i > 0; i--)
        {
            _body[i] = new Pixel(_body[i - 1].Location, bodyColor);
        }

        Head = new Pixel(Head.Location.Move(Direction), Head.Color);

        if (Head.Location.X < 0
            || Head.Location.Y < 0
            || Head.Location.X >= Console.WindowWidth
            || Head.Location.Y >= Console.WindowHeight)
        {
            return false;
        }

        return true;
    }

    public void Draw()
    {
        foreach (var p in Body)
        {
            p.Draw();
        }
    }

    public void TurnLeft()
        => Direction = Direction.LeftOf();

    public void TurnRight()
        => Direction = Direction.RightOf();

    public void FoundBerry()
    {
        Score++;
        if (_last is not null)
        {
            _body.Add(_last);
        }

    }
}

public enum Direction
{
    Up,
    Left,
    Down,
    Right,
}

[DebuggerDisplay("({Location.X}, {Location.Y}) - {Color}")]
public class Pixel
{
    public Point Location { get; }
    public ConsoleColor Color { get; }

    public Pixel(Point location, ConsoleColor color)
    {
        Location = location;
        Color = color;
    }

    public void Draw()
    {
        Console.SetCursorPosition(Location.X, Location.Y);
        Console.ForegroundColor = Color;
        Console.Write("█");
        Console.SetCursorPosition(0, 0);
    }
}
