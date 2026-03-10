using System.Text.Json.Serialization;

namespace Muzjiki_Server;

public class Envelope
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("payload")]
    public string Payload { get; set; } = string.Empty;
}
