using System.Drawing;

namespace Snakes;

public record PlayerState(
    string Id,
    bool HumanControlled,
    IList<Point> Body);
