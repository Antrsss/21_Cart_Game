using TwentyOne.Shared.Models;

namespace TwentyOne.Server.Hubs;

/// <summary>
/// Strongly typed client interface for SignalR hub.
/// Defines all methods that the server can call on clients.
/// </summary>
public interface IGameClient
{
    /// <summary>
    /// Notifies client of updated game state.
    /// </summary>
    Task GameStateUpdated(GameStateDto state);

    /// <summary>
    /// Notifies client that a player has joined the game.
    /// </summary>
    Task PlayerJoined(string playerName, PlayerPosition position);

    /// <summary>
    /// Notifies client that the game has ended with winner information.
    /// </summary>
    Task GameEnded(GameStateDto state, string winnerMessage);

    /// <summary>
    /// Sends an error message to the client.
    /// </summary>
    Task ErrorMessage(string message);

    /// <summary>
    /// Notifies client whose turn it is.
    /// </summary>
    Task PlayerTurn(string playerName);
}

