using TwentyOne.Shared.Models;

namespace TwentyOne.Server.GameLogic;

/// <summary>
/// Manages multiple game instances (rooms) in a thread-safe manner.
/// Each room has its own game instance and player information.
/// </summary>
public class GameManager
{
    private readonly Dictionary<string, (TwentyOneGame Game, Dictionary<string, PlayerPosition> Players)> _games = new();
    private readonly object _lock = new();

    /// <summary>
    /// Gets or creates a game instance for the specified room.
    /// </summary>
    public TwentyOneGame GetOrCreateGame(string roomId)
    {
        lock (_lock)
        {
            if (!_games.ContainsKey(roomId))
            {
                var game = new TwentyOneGame();
                game.Initialize();
                _games[roomId] = (game, new Dictionary<string, PlayerPosition>());
            }
            return _games[roomId].Game;
        }
    }

    /// <summary>
    /// Adds a player to a room and assigns their position.
    /// </summary>
    public PlayerPosition? AddPlayerToRoom(string roomId, string playerName)
    {
        lock (_lock)
        {
            if (!_games.ContainsKey(roomId))
            {
                var newGame = new TwentyOneGame();
                newGame.Initialize();
                _games[roomId] = (newGame, new Dictionary<string, PlayerPosition>());
            }

            var (game, players) = _games[roomId];

            if (players.ContainsKey(playerName))
            {
                return players[playerName];
            }

            PlayerPosition position;
            if (players.Count == 0)
            {
                position = PlayerPosition.Player1;
            }
            else if (players.Count == 1)
            {
                position = PlayerPosition.Player2;
            }
            else
            {
                // Room is full
                return null;
            }

            players[playerName] = position;
            game.AddPlayer(playerName, position);

            return position;
        }
    }

    /// <summary>
    /// Removes a player from a room.
    /// </summary>
    public void RemovePlayerFromRoom(string roomId, string playerName)
    {
        lock (_lock)
        {
            if (_games.ContainsKey(roomId))
            {
                _games[roomId].Players.Remove(playerName);
                
                if (_games[roomId].Players.Count == 0)
                {
                    _games.Remove(roomId);
                }
            }
        }
    }

    /// <summary>
    /// Gets all players in a room with their positions.
    /// </summary>
    public Dictionary<string, PlayerPosition> GetPlayersInRoom(string roomId)
    {
        lock (_lock)
        {
            if (_games.ContainsKey(roomId))
            {
                return new Dictionary<string, PlayerPosition>(_games[roomId].Players);
            }
            return new Dictionary<string, PlayerPosition>();
        }
    }

    /// <summary>
    /// Gets the player position for a given player in a room.
    /// </summary>
    public PlayerPosition? GetPlayerPosition(string roomId, string playerName)
    {
        lock (_lock)
        {
            if (_games.ContainsKey(roomId) && _games[roomId].Players.ContainsKey(playerName))
            {
                return _games[roomId].Players[playerName];
            }
            return null;
        }
    }

    /// <summary>
    /// Removes a game room entirely.
    /// </summary>
    public void RemoveRoom(string roomId)
    {
        lock (_lock)
        {
            _games.Remove(roomId);
        }
    }
}

