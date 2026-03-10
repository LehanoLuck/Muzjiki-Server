using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Muzjiki_Server;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseWebSockets();

var connectionManager = new ConnectionManager();
var matchmakingManager = new MatchmakingManager();
var gameSessions = new List<GameSession>();
var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
};

app.Map("/ws", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("WebSocket connection expected.");
        return;
    }

    var socket = await context.WebSockets.AcceptWebSocketAsync();
    var connectionId = connectionManager.Add(socket);
    Console.WriteLine($"Client connected: {connectionId}");

    try
    {
        while (socket.State == WebSocketState.Open)
        {
            var message = await ReceiveTextMessageAsync(socket, context.RequestAborted);
            if (message is null)
            {
                break;
            }

            Console.WriteLine($"Received from {connectionId}: {message}");

            var envelope = TryDeserializeEnvelope(message, jsonOptions);
            if (envelope is null)
            {
                continue;
            }

            switch (envelope.Type)
            {
                case "QueueQuickMatch":
                    var queued = matchmakingManager.EnqueueQuickMatch(connectionId);
                    Console.WriteLine(queued
                        ? $"Player queued: {connectionId}"
                        : $"Player already in queue: {connectionId}");

                    var gameSession = matchmakingManager.TryCreateMatch(connectionManager);
                    if (gameSession is not null)
                    {
                        gameSessions.Add(gameSession);
                        Console.WriteLine($"Match created: {gameSession.Player1ConnectionId} vs {gameSession.Player2ConnectionId}");
                    }

                    break;
                case "Hello":
                    Console.WriteLine($"Hello from: {connectionId}");
                    break;
                default:
                    Console.WriteLine($"Unknown envelope type: {envelope.Type}");
                    break;
            }
        }
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine($"Connection cancelled: {connectionId}");
    }
    catch (WebSocketException ex)
    {
        Console.WriteLine($"WebSocket error ({connectionId}): {ex.Message}");
    }
    finally
    {
        matchmakingManager.Remove(connectionId);
        connectionManager.Remove(connectionId);
        gameSessions.RemoveAll(session =>
            session.Player1ConnectionId == connectionId || session.Player2ConnectionId == connectionId);

        await TryCloseSocketAsync(socket);
        socket.Dispose();
        Console.WriteLine($"Client disconnected: {connectionId}");
    }
});

app.Run();

static Envelope? TryDeserializeEnvelope(string message, JsonSerializerOptions options)
{
    try
    {
        return JsonSerializer.Deserialize<Envelope>(message, options);
    }
    catch (JsonException)
    {
        return null;
    }
}

static async Task<string?> ReceiveTextMessageAsync(WebSocket socket, CancellationToken cancellationToken)
{
    var buffer = new byte[1024];
    using var stream = new MemoryStream();

    while (true)
    {
        var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

        if (result.MessageType == WebSocketMessageType.Close)
        {
            return null;
        }

        stream.Write(buffer, 0, result.Count);

        if (result.EndOfMessage)
        {
            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }
}

static async Task TryCloseSocketAsync(WebSocket socket)
{
    if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
    {
        try
        {
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", CancellationToken.None);
        }
        catch (WebSocketException)
        {
            // Ignore close handshake exceptions for already-closed sockets.
        }
    }
}
