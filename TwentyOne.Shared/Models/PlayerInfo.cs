namespace TwentyOne.Shared.Models;

/// <summary>
/// Information about a player in the game.
/// </summary>
public class PlayerInfo
{
    public string Name { get; set; } = string.Empty;
    public PlayerPosition Position { get; set; }
    public List<CardDto> Hand { get; set; } = new();
    public int HandValue { get; set; }
    public bool HasBusted { get; set; }
    public bool HasNatural { get; set; }
}

/// <summary>
/// Player position in the game (Player 1 or Player 2).
/// </summary>
public enum PlayerPosition
{
    Player1,
    Player2
}

