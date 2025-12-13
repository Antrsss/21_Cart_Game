namespace TwentyOne.Shared.Models;

/// <summary>
/// Represents the current state of the game.
/// </summary>
public enum GameStatus
{
    WaitingForPlayers,
    Dealing,
    PlayerTurn,
    DealerTurn,
    Finished
}

