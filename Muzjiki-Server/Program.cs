using Muzjiki_Server;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseWebSockets();

ConnectionManager connectionManager = new ConnectionManager();

Queue<WebSocket> matchmakingQueue = new();

app.Map("/ws", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("WebSocket connection expected.");
        return;
    }

    using var ws = await context.WebSockets.AcceptWebSocketAsync();
    Console.WriteLine("Client connected");
    connectionManager.Connections.Add(ws);

    var buffer = new byte[1024];

    while (ws.State == WebSocketState.Open)
    {
        var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        if (result.CloseStatus.HasValue)
        {
            await ws.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            break;
        }

        var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
        var envelope = JsonSerializer.Deserialize<Envelope>(json);

        switch (envelope?.Type)
        {
            case "QueueQuickMatch":
                Console.WriteLine("Player queued");
                matchmakingQueue.Enqueue(ws);
                TryStartMatch();
                break;
            default:
                break;
        }

        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
        Console.WriteLine($"Received: {message}");
    }
});

void TryStartMatch()
{
    if (matchmakingQueue.Count >= 2)
    {
        var p1 = matchmakingQueue.Dequeue();
        var p2 = matchmakingQueue.Dequeue();

        Console.WriteLine("Match created!");
    }
}

app.Run();
