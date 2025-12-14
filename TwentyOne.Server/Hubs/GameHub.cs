using Microsoft.AspNetCore.SignalR;
using TwentyOne.Server.GameLogic;
using TwentyOne.Shared.Models;

namespace TwentyOne.Server.Hubs;

/// <summary>
/// SignalR hub for real-time game communication.
/// Uses strongly typed hub with IGameClient interface.
/// Manages game rooms using SignalR groups.
/// </summary>
public class GameHub : Hub<IGameClient>
{
    private readonly GameManager _gameManager;

    // Track which connection belongs to which player
    private static readonly Dictionary<string, string> _connectionToPlayer = new();
    private static readonly Dictionary<string, string> _playerToConnection = new();
    private readonly object _connectionLock = new();

    public GameHub(GameManager gameManager)
    {
        _gameManager = gameManager;
    }

    /// <summary>
    /// Allows a player to join a game room.
    /// First player becomes Player1, second becomes Player2.
    /// </summary>
    public async Task JoinGame(string roomId, string playerName)
    {
        if (string.IsNullOrWhiteSpace(roomId) || string.IsNullOrWhiteSpace(playerName))
        {
            await Clients.Caller.ErrorMessage("Room ID and player name are required.");
            return;
        }

        // Add connection to SignalR group for this room
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

        // Track connection -> player mapping
        lock (_connectionLock)
        {
            // Remove old connection if player reconnects
            if (_playerToConnection.ContainsKey(playerName))
            {
                var oldConnectionId = _playerToConnection[playerName];
                _connectionToPlayer.Remove(oldConnectionId);
            }
            _connectionToPlayer[Context.ConnectionId] = playerName;
            _playerToConnection[playerName] = Context.ConnectionId;
        }

        // Add player to game
        var position = _gameManager.AddPlayerToRoom(roomId, playerName);

        if (position == null)
        {
            await Clients.Caller.ErrorMessage("Room is full. Maximum 2 players allowed.");
            return;
        }

        var game = _gameManager.GetOrCreateGame(roomId);

        // Notify all clients in the room that a player joined
        await Clients.Group(roomId).PlayerJoined(playerName, position.Value);

        // Send personalized game state to the new player
        var gameState = game.GetGameStateForPlayer(roomId, playerName);
        await Clients.Caller.GameStateUpdated(gameState);

        // If both players are present, start the game
        if (game.CanStartGame())
        {
            game.StartGame();
            
            // Send personalized state to each player
            var baseState = game.GetGameState(roomId);
            if (baseState.Player1 != null)
            {
                var state1 = game.GetGameStateForPlayer(roomId, baseState.Player1.Name);
                lock (_connectionLock)
                {
                    if (_playerToConnection.TryGetValue(baseState.Player1.Name, out var conn1))
                    {
                        Clients.Client(conn1).GameStateUpdated(state1);
                    }
                }
            }
            if (baseState.Player2 != null)
            {
                var state2 = game.GetGameStateForPlayer(roomId, baseState.Player2.Name);
                lock (_connectionLock)
                {
                    if (_playerToConnection.TryGetValue(baseState.Player2.Name, out var conn2))
                    {
                        Clients.Client(conn2).GameStateUpdated(state2);
                    }
                }
            }
            
            // Notify whose turn it is
            if (baseState.CurrentPlayerTurn.HasValue)
            {
                var currentPlayer = baseState.CurrentPlayerTurn == PlayerPosition.Player1 
                    ? baseState.Player1?.Name 
                    : baseState.Player2?.Name;
                if (currentPlayer != null)
                {
                    await Clients.Group(roomId).PlayerTurn(currentPlayer);
                }
            }
        }
    }
    
    /// <summary>
    /// Processes a player action (Hit or Stand) during their turn.
    /// </summary>
    public async Task PlayerAction(string roomId, string playerName, PlayerAction action)
    {
        var game = _gameManager.GetOrCreateGame(roomId);

        // Get player position from game manager
        var position = _gameManager.GetPlayerPosition(roomId, playerName);
        if (!position.HasValue)
        {
            await Clients.Caller.ErrorMessage("You are not a player in this game.");
            return;
        }

        // Process the action
        bool success = game.ProcessPlayerAction(position.Value, action);
        if (!success)
        {
            await Clients.Caller.ErrorMessage("Invalid action. It may not be your turn or the game is not in progress.");
            return;
        }

        // Send personalized game state to each player
        var baseState = game.GetGameState(roomId);
        
        if (baseState.Player1 != null)
        {
            var state1 = game.GetGameStateForPlayer(roomId, baseState.Player1.Name);
            lock (_connectionLock)
            {
                if (_playerToConnection.TryGetValue(baseState.Player1.Name, out var conn1))
                {
                    Clients.Client(conn1).GameStateUpdated(state1);
                }
            }
        }
        if (baseState.Player2 != null)
        {
            var state2 = game.GetGameStateForPlayer(roomId, baseState.Player2.Name);
            lock (_connectionLock)
            {
                if (_playerToConnection.TryGetValue(baseState.Player2.Name, out var conn2))
                {
                    Clients.Client(conn2).GameStateUpdated(state2);
                }
            }
        }

        // If game is finished, notify clients
        if (baseState.IsGameOver)
        {
            await Clients.Group(roomId).GameEnded(baseState, baseState.WinnerMessage ?? "Game Over");
        }
        else if (baseState.Status == GameStatus.PlayerTurn && baseState.CurrentPlayerTurn.HasValue)
        {
            // Notify whose turn it is
            var currentPlayer = baseState.CurrentPlayerTurn == PlayerPosition.Player1 
                ? baseState.Player1?.Name 
                : baseState.Player2?.Name;
            if (currentPlayer != null)
            {
                await Clients.Group(roomId).PlayerTurn(currentPlayer);
            }
        }
    }

    /// <summary>
    /// Restarts the game in the current room.
    /// </summary>
    public async Task RestartGame(string roomId)
    {
        var game = _gameManager.GetOrCreateGame(roomId);
        
        var players = _gameManager.GetPlayersInRoom(roomId);
        
        game.Reset();
        
        foreach (var (playerName, position) in players)
        {
            game.AddPlayer(playerName, position);
        }
        
        if (game.CanStartGame())
        {
            game.StartGame();
        }

        var baseState = game.GetGameState(roomId);
        
        if (baseState.Player1 != null)
        {
            var state1 = game.GetGameStateForPlayer(roomId, baseState.Player1.Name);
            lock (_connectionLock)
            {
                if (_playerToConnection.TryGetValue(baseState.Player1.Name, out var conn1))
                {
                    Clients.Client(conn1).GameStateUpdated(state1);
                }
            }
        }
        if (baseState.Player2 != null)
        {
            var state2 = game.GetGameStateForPlayer(roomId, baseState.Player2.Name);
            lock (_connectionLock)
            {
                if (_playerToConnection.TryGetValue(baseState.Player2.Name, out var conn2))
                {
                    Clients.Client(conn2).GameStateUpdated(state2);
                }
            }
        }

        if (baseState.Status == GameStatus.PlayerTurn && baseState.CurrentPlayerTurn.HasValue)
        {
            var currentPlayer = baseState.CurrentPlayerTurn == PlayerPosition.Player1 
                ? baseState.Player1?.Name 
                : baseState.Player2?.Name;
            if (currentPlayer != null)
            {
                await Clients.Group(roomId).PlayerTurn(currentPlayer);
            }
        }
    }

    /// <summary>
    /// Called when a client disconnects. Removes them from the room.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        lock (_connectionLock)
        {
            if (_connectionToPlayer.TryGetValue(Context.ConnectionId, out var playerName))
            {
                _connectionToPlayer.Remove(Context.ConnectionId);
                _playerToConnection.Remove(playerName);
            }
        }
        
        await base.OnDisconnectedAsync(exception);
    }
}

