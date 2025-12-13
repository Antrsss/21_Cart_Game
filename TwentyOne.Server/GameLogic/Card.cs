using TwentyOne.Shared.Models;

namespace TwentyOne.Server.GameLogic;

/// <summary>
/// Internal representation of a playing card.
/// </summary>
internal class Card
{
    public CardSuit Suit { get; set; }
    public CardRank Rank { get; set; }

    public Card(CardSuit suit, CardRank rank)
    {
        Suit = suit;
        Rank = rank;
    }

    /// <summary>
    /// Gets the numeric value of the card for Twenty One game.
    /// Face cards (Jack, Queen, King) are worth 10.
    /// Ace is worth 1 (will be handled separately for soft/hard totals).
    /// </summary>
    public int GetBaseValue()
    {
        return Rank switch
        {
            CardRank.Jack or CardRank.Queen or CardRank.King => 10,
            CardRank.Ace => 1,
            _ => (int)Rank
        };
    }
}

