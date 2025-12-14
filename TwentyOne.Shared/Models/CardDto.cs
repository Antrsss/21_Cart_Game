namespace TwentyOne.Shared.Models;

/// <summary>
/// Represents a playing card with suit, rank, and visibility state.
/// </summary>
public class CardDto
{
    public CardSuit Suit { get; set; }
    public CardRank Rank { get; set; }
    public bool IsFaceUp { get; set; }
}

/// <summary>
/// Card suit enumeration.
/// </summary>
public enum CardSuit
{
    Hearts,
    Diamonds,
    Clubs,
    Spades
}

/// <summary>
/// Card rank enumeration.
/// </summary>
public enum CardRank
{
    Ace = 1,
    Two = 2,
    Three = 3,
    Four = 4,
    Five = 5,
    Six = 6,
    Seven = 7,
    Eight = 8,
    Nine = 9,
    Ten = 10,
    Jack = 11,
    Queen = 12,
    King = 13
}