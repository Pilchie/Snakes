namespace Snakes;

public record PlayerState(
    string Id,
    string Name,
    bool HumanControlled,
    IList<Point> Body);
