using System;
using System.Diagnostics;
using System.Drawing;

namespace Snakes;


[DebuggerDisplay("({Location.X}, {Location.Y}) - {Color}")]
public class Pixel
{
    public Point Location { get; }
    public KnownColor Color { get; }

    public Pixel(Point location, KnownColor color)
    {
        Location = location;
        Color = color;
    }
}
