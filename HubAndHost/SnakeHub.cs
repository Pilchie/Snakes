using Microsoft.AspNetCore.SignalR;
using Orleans;

namespace Snakes;

public class GameObserver : IGameObserver
{
    private readonly IHubContext<SnakeHub, IGameObserver> _hubContext;
    private readonly IClusterClient _clusterClient;

    public GameObserver(
        IHubContext<SnakeHub, IGameObserver> hubContext,
        IClusterClient clusterClient)
    {
        _hubContext = hubContext;
        _clusterClient = clusterClient;
    }

    public async Task OnBoardSizeChanged(Size newSize)
    {
        await _hubContext.Clients.All.OnBoardSizeChanged(newSize);
    }

    public async Task OnDied(string id)
    {
        await _hubContext.Clients.Client(id).OnDied(id);
    }

    public async Task OnExpectedPlayerCountChanged(int newCount)
    {
        await _hubContext.Clients.All.OnExpectedPlayerCountChanged(newCount);
    }

    public async Task OnNewRound()
    {
        // Won't be called by the grain, synthesize the arguments to the hub clients.
        // Consider moving this into the grain, and simplifying the interface.
        var game = _clusterClient.GetGrain<IGame>(Guid.Empty);
        var playerGrains = await game.GetPlayers();
        var berries = await game.GetBerryPositions();
        var players = new List<PlayerState>();
        foreach (var p in playerGrains)
        {
            players.Add(new PlayerState(
                p.GetPrimaryKeyString(),
                await p.GetName(),
                await p.IsHumanControlled(),
                await p.GetBody()));
        }
        await this.OnNewRound(players, berries);
    }

    public async Task OnNewRound(IList<PlayerState> players, IEnumerable<Point> berries)
    {
        await _hubContext.Clients.All.OnNewRound(players, berries);
    }

    public async Task OnPlayerJoined(int newCount)
    {
        await _hubContext.Clients.All.OnPlayerJoined(newCount);
    }

    public async Task OnScoreChanged(string id, int newScore)
    {
        await _hubContext.Clients.Client(id).OnScoreChanged(id, newScore);
    }

    public async Task OnStateChanged(GameState newState)
    {
        await _hubContext.Clients.All.OnStateChanged(newState);
    }
}

public class SnakeHub : Hub<IGameObserver>
{
    private readonly IClusterClient _clusterClient;
    private readonly IHubContext<SnakeHub, IGameObserver> _hubContext;

    public SnakeHub(IClusterClient clusterClient,
        IHubContext<SnakeHub, IGameObserver> hubContext)
    {
        _clusterClient = clusterClient;
        _hubContext = hubContext;
    }

    public async Task<GameState> GetCurrentState()
    {
        var game = _clusterClient.GetGrain<IGame>(Guid.Empty);
        if (!Context.Items.ContainsKey("GameObserver"))
        {
            var go = new GameObserver(_hubContext, _clusterClient);
            Context.Items.Add("GameObserver", go);
            var gor = await _clusterClient.CreateObjectReference<IGameObserver>(go);
            await game.Subscribe(gor);
        }

        return await game.GetCurrentState();
    }

    public async Task InitializeNewGame(Size boardSize, int expectedPlayerCount)
    {
        var game = _clusterClient.GetGrain<IGame>(Guid.Empty);
        await game.InitializeNewGame(boardSize, expectedPlayerCount);
    }

    public async Task<LobbyState> GetLobbyState()
    {
        var game = _clusterClient.GetGrain<IGame>(Guid.Empty);
        var expected = await game.GetExpectedPlayers();
        var players = await game.GetPlayers();
        var boardSize = await game.GetBoardSize();
        return new LobbyState(players.Count(), expected, boardSize);
    }

    public async Task<string> JoinGame(string playerName)
    {
        var game = _clusterClient.GetGrain<IGame>(Guid.Empty);
        var self = _clusterClient.GetGrain<IPlayer>(Context.ConnectionId);
        await self.SetHumanControlled(true);
        await self.SetName(playerName);
        await self.JoinGame(game);
        return self.GetPrimaryKeyString();
    }

    public async Task StartGame()
    {
        var game = _clusterClient.GetGrain<IGame>(Guid.Empty);
        await game.Start();
    }

    public async Task TurnLeft()
    {
        var self = _clusterClient.GetGrain<IPlayer>(Context.ConnectionId);
        await self.TurnLeft();
    }
    public async Task TurnRight()
    {
        var self = _clusterClient.GetGrain<IPlayer>(Context.ConnectionId);
        await self.TurnRight();
    }
}
