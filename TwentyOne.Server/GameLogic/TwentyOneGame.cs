using TwentyOne.Shared.Models;

namespace TwentyOne.Server.GameLogic;

/// <summary>
/// Core game logic for Twenty One card game.
/// Handles deck management, card dealing, hand evaluation, and game state.
/// </summary>
public class TwentyOneGame
{
    private List<Card> _deck = new();
    private readonly Random _random = new();
    private PlayerInfo? _player1;
    private PlayerInfo? _player2;
    private PlayerInfo _dealer = new();
    private GameStatus _status = GameStatus.WaitingForPlayers;
    private PlayerPosition? _currentPlayerTurn;

    public GameStatus Status => _status;
    public PlayerPosition? CurrentPlayerTurn => _currentPlayerTurn;

    /// <summary>
    /// Initializes a new game with a fresh shuffled deck.
    /// </summary>
    public void Initialize()
    {
        _deck = CreateDeck();
        ShuffleDeck();
        _player1 = null;
        _player2 = null;
        _dealer = new PlayerInfo { Name = "Dealer", Hand = new List<CardDto>() };
        _status = GameStatus.WaitingForPlayers;
        _currentPlayerTurn = null;
    }

    /// <summary>
    /// Creates a standard 52-card deck.
    /// </summary>
    private List<Card> CreateDeck()
    {
        var deck = new List<Card>();
        foreach (CardSuit suit in Enum.GetValues<CardSuit>())
        {
            foreach (CardRank rank in Enum.GetValues<CardRank>())
            {
                deck.Add(new Card(suit, rank));
            }
        }
        return deck;
    }

    /// <summary>
    /// Shuffles the deck using Fisher-Yates algorithm.
    /// </summary>
    private void ShuffleDeck()
    {
        for (int i = _deck.Count - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (_deck[i], _deck[j]) = (_deck[j], _deck[i]);
        }
    }

    /// <summary>
    /// Adds a player to the game. First player becomes Player1, second becomes Player2.
    /// </summary>
    public bool AddPlayer(string playerName, PlayerPosition position)
    {
        if (position == PlayerPosition.Player1)
        {
            if (_player1 != null) return false;
            _player1 = new PlayerInfo
            {
                Name = playerName,
                Position = PlayerPosition.Player1,
                Hand = new List<CardDto>()
            };
        }
        else if (position == PlayerPosition.Player2)
        {
            if (_player2 != null) return false;
            _player2 = new PlayerInfo
            {
                Name = playerName,
                Position = PlayerPosition.Player2,
                Hand = new List<CardDto>()
            };
        }

        return true;
    }

    /// <summary>
    /// Checks if both players have joined and game can start.
    /// </summary>
    public bool CanStartGame()
    {
        return _player1 != null && _player2 != null && _status == GameStatus.WaitingForPlayers;
    }

    /// <summary>
    /// Starts the game by dealing initial cards to all players and dealer.
    /// </summary>
    public void StartGame()
    {
        if (!CanStartGame()) return;

        _status = GameStatus.Dealing;

        // Deal 2 cards to each player (face up)
        DealCard(_player1!, faceUp: true);
        DealCard(_player1!, faceUp: true);
        DealCard(_player2!, faceUp: true);
        DealCard(_player2!, faceUp: true);

        // Deal 2 cards to dealer (one face up, one face down)
        DealCard(_dealer, faceUp: true);
        DealCard(_dealer, faceUp: false);

        // Check for naturals (21 with first two cards)
        CheckForNaturals();

        // Start with Player 1's turn
        _status = GameStatus.PlayerTurn;
        _currentPlayerTurn = PlayerPosition.Player1;
    }

    /// <summary>
    /// Deals a card from the deck to a player's hand.
    /// </summary>
    private void DealCard(PlayerInfo player, bool faceUp)
    {
        if (_deck.Count == 0)
        {
            // Reshuffle if deck is empty (shouldn't happen in normal play)
            _deck = CreateDeck();
            ShuffleDeck();
        }

        var card = _deck[0];
        _deck.RemoveAt(0);

        player.Hand.Add(new CardDto
        {
            Suit = card.Suit,
            Rank = card.Rank,
            IsFaceUp = faceUp
        });

        UpdateHandValue(player);
    }

    /// <summary>
    /// Calculates the hand value, handling Ace as 1 or 11 (whichever is better).
    /// </summary>
    private void UpdateHandValue(PlayerInfo player)
    {
        int value = 0;
        int aceCount = 0;

        foreach (var card in player.Hand.Where(c => c.IsFaceUp))
        {
            int cardValue = card.Rank switch
            {
                CardRank.Jack or CardRank.Queen or CardRank.King => 10,
                CardRank.Ace => 1,
                _ => (int)card.Rank
            };

            value += cardValue;
            if (card.Rank == CardRank.Ace)
                aceCount++;
        }

        // Optimize Ace values: add 10 for each Ace if it doesn't cause bust
        for (int i = 0; i < aceCount; i++)
        {
            if (value + 10 <= 21)
                value += 10;
        }

        player.HandValue = value;
        player.HasBusted = value > 21;
    }

    /// <summary>
    /// Checks if any player or dealer has a natural (21 with first two cards).
    /// </summary>
    private void CheckForNaturals()
    {
        if (_player1!.Hand.Count == 2 && _player1.HandValue == 21)
            _player1.HasNatural = true;
        if (_player2!.Hand.Count == 2 && _player2.HandValue == 21)
            _player2.HasNatural = true;
        if (_dealer.Hand.Count == 2 && _dealer.HandValue == 21)
            _dealer.HasNatural = true;
    }

    /// <summary>
    /// Processes a player action (Hit or Stand) during their turn.
    /// </summary>
    public bool ProcessPlayerAction(PlayerPosition position, PlayerAction action)
    {
        if (_status != GameStatus.PlayerTurn || _currentPlayerTurn != position)
            return false;

        var player = position == PlayerPosition.Player1 ? _player1 : _player2;
        if (player == null) return false;

        if (action == PlayerAction.Hit)
        {
            DealCard(player, faceUp: true);
            
            if (player.HasBusted)
            {
                AdvanceTurn();
            }
        }
        else if (action == PlayerAction.Stand)
        {
            AdvanceTurn();
        }

        return true;
    }

    /// <summary>
    /// Advances the game turn to the next player or dealer.
    /// </summary>
    private void AdvanceTurn()
    {
        if (_currentPlayerTurn == PlayerPosition.Player1)
        {
            _currentPlayerTurn = PlayerPosition.Player2;
            if (_player2!.HasBusted)
            {
                StartDealerTurn();
            }
        }
        else if (_currentPlayerTurn == PlayerPosition.Player2)
        {
            StartDealerTurn();
        }
    }

    /// <summary>
    /// Starts the dealer's turn. Dealer reveals hidden card and follows fixed rules.
    /// </summary>
    private void StartDealerTurn()
    {
        _status = GameStatus.DealerTurn;
        _currentPlayerTurn = null;

        foreach (var card in _dealer.Hand)
        {
            card.IsFaceUp = true;
        }
        UpdateHandValue(_dealer);

        // Dealer must hit while hand value < 17
        while (_dealer.HandValue < 17 && !_dealer.HasBusted)
        {
            DealCard(_dealer, faceUp: true);
        }

        DetermineWinner();
    }

    /// <summary>
    /// Determines the winner based on game rules and updates game status.
    /// </summary>
    private void DetermineWinner()
    {
        _status = GameStatus.Finished;

        bool player1Natural = _player1!.HasNatural && !_dealer.HasNatural;
        bool player2Natural = _player2!.HasNatural && !_dealer.HasNatural;
        bool dealerNatural = _dealer.HasNatural && (!_player1.HasNatural || !_player2.HasNatural);

        if (dealerNatural)
        {
            return;
        }

        var results = new List<(PlayerInfo player, string result)>();

        EvaluatePlayerResult(_player1, results);
        EvaluatePlayerResult(_player2, results);

        var messages = results.Select(r => $"{r.player.Name}: {r.result}").ToList();
    }

    /// <summary>
    /// Evaluates a single player's result against the dealer.
    /// </summary>
    private void EvaluatePlayerResult(PlayerInfo player, List<(PlayerInfo, string)> results)
    {
        if (player.HasNatural && !_dealer.HasNatural)
        {
            results.Add((player, "Wins (Natural Blackjack!)"));
        }
        else if (player.HasBusted)
        {
            results.Add((player, "Loses (Busted)"));
        }
        else if (_dealer.HasBusted)
        {
            results.Add((player, "Wins (Dealer Busted)"));
        }
        else if (player.HandValue > _dealer.HandValue)
        {
            results.Add((player, "Wins"));
        }
        else if (player.HandValue < _dealer.HandValue)
        {
            results.Add((player, "Loses"));
        }
        else
        {
            results.Add((player, "Push (Tie)"));
        }
    }

    /// <summary>
    /// Gets the current game state as a DTO for transmission to clients.
    /// </summary>
    public GameStateDto GetGameState(string roomId)
    {
        var winnerMessage = "";
        if (_status == GameStatus.Finished)
        {
            var messages = new List<string>();
            if (_player1 != null)
            {
                var result = GetPlayerResult(_player1);
                messages.Add($"{_player1.Name}: {result}");
            }
            if (_player2 != null)
            {
                var result = GetPlayerResult(_player2);
                messages.Add($"{_player2.Name}: {result}");
            }
            winnerMessage = string.Join(" | ", messages);
        }

        return new GameStateDto
        {
            RoomId = roomId,
            Status = _status,
            Player1 = _player1,
            Player2 = _player2,
            Dealer = _dealer,
            CurrentPlayerTurn = _currentPlayerTurn,
            WinnerMessage = winnerMessage,
            IsGameOver = _status == GameStatus.Finished
        };
    }

    /// <summary>
    /// Gets the game state for a specific player, hiding opponent's cards.
    /// </summary>
    public GameStateDto GetGameStateForPlayer(string roomId, string playerName)
    {
        var baseState = GetGameState(roomId);
        
        // Create a copy to avoid modifying the original
        var playerState = new GameStateDto
        {
            RoomId = baseState.RoomId,
            Status = baseState.Status,
            CurrentPlayerTurn = baseState.CurrentPlayerTurn,
            WinnerMessage = baseState.WinnerMessage,
            IsGameOver = baseState.IsGameOver,
            Dealer = ClonePlayerInfo(baseState.Dealer)
        };
        
        // Clone player info and hide opponent's cards
        if (baseState.Player1 != null)
        {
            if (baseState.Player1.Name == playerName)
            {
                // This is the requesting player - show their cards
                playerState.Player1 = ClonePlayerInfo(baseState.Player1);
            }
            else
            {
                // This is the opponent - hide their cards
                playerState.Player1 = ClonePlayerInfoWithHiddenCards(baseState.Player1);
            }
        }
        
        if (baseState.Player2 != null)
        {
            if (baseState.Player2.Name == playerName)
            {
                // This is the requesting player - show their cards
                playerState.Player2 = ClonePlayerInfo(baseState.Player2);
            }
            else
            {
                // This is the opponent - hide their cards
                playerState.Player2 = ClonePlayerInfoWithHiddenCards(baseState.Player2);
            }
        }
        
        return playerState;
    }
    
    /// <summary>
    /// Creates a deep copy of PlayerInfo.
    /// </summary>
    private PlayerInfo ClonePlayerInfo(PlayerInfo source)
    {
        return new PlayerInfo
        {
            Name = source.Name,
            Position = source.Position,
            Hand = source.Hand.Select(c => new CardDto
            {
                Suit = c.Suit,
                Rank = c.Rank,
                IsFaceUp = c.IsFaceUp
            }).ToList(),
            HandValue = source.HandValue,
            HasBusted = source.HasBusted,
            HasNatural = source.HasNatural
        };
    }
    
    /// <summary>
    /// Creates a copy of PlayerInfo with all cards hidden.
    /// </summary>
    private PlayerInfo ClonePlayerInfoWithHiddenCards(PlayerInfo source)
    {
        return new PlayerInfo
        {
            Name = source.Name,
            Position = source.Position,
            Hand = source.Hand.Select(c => new CardDto
            {
                Suit = c.Suit,
                Rank = c.Rank,
                IsFaceUp = false
            }).ToList(),
            HandValue = 0,
            HasBusted = source.HasBusted,
            HasNatural = false
        };
    }

    /// <summary>
    /// Gets the result message for a specific player.
    /// </summary>
    private string GetPlayerResult(PlayerInfo player)
    {
        if (player.HasNatural && !_dealer.HasNatural)
            return "Wins (Natural Blackjack!)";
        if (player.HasBusted)
            return "Loses (Busted)";
        if (_dealer.HasBusted)
            return "Wins (Dealer Busted)";
        if (player.HandValue > _dealer.HandValue)
            return "Wins";
        if (player.HandValue < _dealer.HandValue)
            return "Loses";
        return "Push (Tie)";
    }

    /// <summary>
    /// Resets the game for a new round.
    /// </summary>
    public void Reset()
    {
        Initialize();
    }
}

