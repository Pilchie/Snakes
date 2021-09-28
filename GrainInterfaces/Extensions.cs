using System.Drawing;
using System;

namespace Snakes;

public static class Extensions
{
    public static Point OnScreen(this Random random, int border, Size size)
        => new Point(random.Next(border, size.Width - border), random.Next(border, size.Height - border));

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
