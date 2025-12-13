using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Components;
using TwentyOne.Shared.Models;

namespace TwentyOne.Client.Services;

/// <summary>
/// Client-side service for managing SignalR connection and game communication.
/// Handles connection lifecycle, sending actions to server, and receiving updates.
/// </summary>
public class GameClientService : IAsyncDisposable
{
    private HubConnection? _hubConnection;
    private readonly NavigationManager _navigationManager;

    // Events for UI to subscribe to
    public event Action<GameStateDto>? GameStateUpdated;
    public event Action<string, PlayerPosition>? PlayerJoined;
    public event Action<GameStateDto, string>? GameEnded;
    public event Action<string>? ErrorReceived;
    public event Action<string>? PlayerTurnReceived;

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;
    public string? CurrentRoomId { get; private set; }
    public string? CurrentPlayerName { get; private set; }

    public GameClientService(NavigationManager navigationManager)
    {
        _navigationManager = navigationManager;
    }

    /// <summary>
    /// Establishes connection to the SignalR hub.
    /// </summary>
    public async Task ConnectAsync()
    {
        if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
            return;

        // Get server URL from navigation manager or use default
        // In development, server typically runs on different port than client
        var baseUri = _navigationManager.BaseUri;
        var serverBaseUrl = (baseUri.Contains("localhost:5001") || baseUri.Contains("localhost:5002"))
            ? "https://localhost:7000" 
            : _navigationManager.BaseUri.TrimEnd('/');
        var hubUrl = $"{serverBaseUrl}/gamehub";

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .Build();

        // Register handlers for server-to-client messages
        _hubConnection.On<GameStateDto>("GameStateUpdated", (state) =>
        {
            GameStateUpdated?.Invoke(state);
        });

        _hubConnection.On<string, PlayerPosition>("PlayerJoined", (playerName, position) =>
        {
            PlayerJoined?.Invoke(playerName, position);
        });

        _hubConnection.On<GameStateDto, string>("GameEnded", (state, message) =>
        {
            GameEnded?.Invoke(state, message);
        });

        _hubConnection.On<string>("ErrorMessage", (message) =>
        {
            ErrorReceived?.Invoke(message);
        });

        _hubConnection.On<string>("PlayerTurn", (playerName) =>
        {
            PlayerTurnReceived?.Invoke(playerName);
        });

        try
        {
            await _hubConnection.StartAsync();
        }
        catch (Exception ex)
        {
            ErrorReceived?.Invoke($"Failed to connect: {ex.Message}");
        }
    }

    /// <summary>
    /// Disconnects from the SignalR hub.
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
        CurrentRoomId = null;
        CurrentPlayerName = null;
    }

    /// <summary>
    /// Joins a game room with the specified room ID and player name.
    /// </summary>
    public async Task JoinGameAsync(string roomId, string playerName)
    {
        if (_hubConnection == null || _hubConnection.State != HubConnectionState.Connected)
        {
            await ConnectAsync();
        }

        if (_hubConnection != null)
        {
            try
            {
                await _hubConnection.SendAsync("JoinGame", roomId, playerName);
                CurrentRoomId = roomId;
                CurrentPlayerName = playerName;
            }
            catch (Exception ex)
            {
                ErrorReceived?.Invoke($"Failed to join game: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Sends a player action (Hit or Stand) to the server.
    /// </summary>
    public async Task PlayerActionAsync(string roomId, PlayerAction action)
    {
        if (_hubConnection == null || _hubConnection.State != HubConnectionState.Connected)
        {
            ErrorReceived?.Invoke("Not connected to server.");
            return;
        }

        if (string.IsNullOrEmpty(CurrentPlayerName))
        {
            ErrorReceived?.Invoke("Player name not set.");
            return;
        }

        try
        {
            await _hubConnection.SendAsync("PlayerAction", roomId, CurrentPlayerName, action);
        }
        catch (Exception ex)
        {
            ErrorReceived?.Invoke($"Failed to send action: {ex.Message}");
        }
    }

    /// <summary>
    /// Requests a game restart in the current room.
    /// </summary>
    public async Task RestartGameAsync(string roomId)
    {
        if (_hubConnection == null || _hubConnection.State != HubConnectionState.Connected)
        {
            ErrorReceived?.Invoke("Not connected to server.");
            return;
        }

        try
        {
            await _hubConnection.SendAsync("RestartGame", roomId);
        }
        catch (Exception ex)
        {
            ErrorReceived?.Invoke($"Failed to restart game: {ex.Message}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}

