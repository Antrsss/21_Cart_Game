namespace TwentyOne.Shared.Models;

/// <summary>
/// Complete game state transferred between server and clients.
/// </summary>
public class GameStateDto
{
    public string RoomId { get; set; } = string.Empty;
    public GameStatus Status { get; set; }
    public PlayerInfo? Player1 { get; set; }
    public PlayerInfo? Player2 { get; set; }
    public PlayerInfo Dealer { get; set; } = new();
    public PlayerPosition? CurrentPlayerTurn { get; set; }
    public string? WinnerMessage { get; set; }
    public bool IsGameOver { get; set; }
}

