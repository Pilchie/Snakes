using System.Drawing;

namespace Snakes;

public record class LobbyState(
    int CurrentPlayers,
    int ExpectedPlayers,
    Size BoardSize);
