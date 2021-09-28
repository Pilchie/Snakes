using System;
using System.Diagnostics;
using System.Drawing;

namespace Snakes;


[DebuggerDisplay("({Location.X}, {Location.Y}) - {Color}")]
public class Pixel
{
    public Point Location { get; }
    public Color Color { get; }

    public Pixel(Point location, Color color)
    {
        Location = location;
        Color = color;
    }
}
