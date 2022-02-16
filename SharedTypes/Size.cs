namespace Snakes;

public class Size : IEquatable<Size?>
{
    public Size()
    {
        Width = 0;
        Height = 0;
    }

    public Size(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public int Width { get; private set; }

    public int Height { get; private set; }


    public override string? ToString()
    {
        return $"{{{Width},{Height}}})";
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Size);
    }

    public bool Equals(Size? other)
    {
        return other is not null &&
               Width == other.Width &&
               Height == other.Height;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Width, Height);
    }

    public static bool operator ==(Size? left, Size? right)
    {
        return EqualityComparer<Size>.Default.Equals(left, right);
    }

    public static bool operator !=(Size? left, Size? right)
    {
        return !(left == right);
    }
}

public class Point : IEquatable<Point?>
{
    public Point()
    {
        X = 0;
        Y = 0;
    }

    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X { get; private set; }

    public int Y { get; private set; }

    public override string? ToString()
    {
        return $"({X},{Y})";
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Point);
    }

    public bool Equals(Point? other)
    {
        return other is not null &&
               X == other.X &&
               Y == other.Y;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    public static bool operator ==(Point? left, Point? right)
    {
        return EqualityComparer<Point>.Default.Equals(left, right);
    }

    public static bool operator !=(Point? left, Point? right)
    {
        return !(left == right);
    }
}

