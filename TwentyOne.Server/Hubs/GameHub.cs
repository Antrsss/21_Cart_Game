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

        // Send current game state to the new player
        var gameState = game.GetGameState(roomId);
        await Clients.Caller.GameStateUpdated(gameState);

        // If both players are present, start the game
        if (game.CanStartGame())
        {
            game.StartGame();
            var updatedState = game.GetGameState(roomId);
            await Clients.Group(roomId).GameStateUpdated(updatedState);
            
            // Notify whose turn it is
            if (updatedState.CurrentPlayerTurn.HasValue)
            {
                var currentPlayer = updatedState.CurrentPlayerTurn == PlayerPosition.Player1 
                    ? updatedState.Player1?.Name 
                    : updatedState.Player2?.Name;
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

        // Broadcast updated game state to all clients in the room
        var gameState = game.GetGameState(roomId);
        await Clients.Group(roomId).GameStateUpdated(gameState);

        // If game is finished, notify clients
        if (gameState.IsGameOver)
        {
            await Clients.Group(roomId).GameEnded(gameState, gameState.WinnerMessage ?? "Game Over");
        }
        else if (gameState.Status == GameStatus.PlayerTurn && gameState.CurrentPlayerTurn.HasValue)
        {
            // Notify whose turn it is
            var currentPlayer = gameState.CurrentPlayerTurn == PlayerPosition.Player1 
                ? gameState.Player1?.Name 
                : gameState.Player2?.Name;
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
        
        // Get players before reset
        var players = _gameManager.GetPlayersInRoom(roomId);
        
        // Reset the game
        game.Reset();
        
        // Re-add players to the game after reset
        foreach (var (playerName, position) in players)
        {
            game.AddPlayer(playerName, position);
        }
        
        // Start new game if both players are still present
        if (game.CanStartGame())
        {
            game.StartGame();
        }

        var gameState = game.GetGameState(roomId);
        await Clients.Group(roomId).GameStateUpdated(gameState);

        if (gameState.Status == GameStatus.PlayerTurn && gameState.CurrentPlayerTurn.HasValue)
        {
            var currentPlayer = gameState.CurrentPlayerTurn == PlayerPosition.Player1 
                ? gameState.Player1?.Name 
                : gameState.Player2?.Name;
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
        // Note: In a production app, you might want to track which room each connection is in
        // For simplicity, we'll handle cleanup through the game manager when needed
        await base.OnDisconnectedAsync(exception);
    }
}

